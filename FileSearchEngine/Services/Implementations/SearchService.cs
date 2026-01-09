using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Domain.Interfaces;
using FileSearchEngine.Domain.ValueObjects;
using FileSearchEngine.Infrastructure.Repositories.Interfaces;
using FileSearchEngine.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Services.Implementations
{
    public class SearchService : ISearchService
    {
        private readonly IInvertedIndex _invertedIndex;
        private readonly IDocumentRepository _documentRepository;
        private readonly ITextProcessor _textProcessor;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<SearchService> _logger;
        
        public SearchService(
            IInvertedIndex invertedIndex,
            IDocumentRepository documentRepository,
            ITextProcessor textProcessor,
            IFileStorageService fileStorageService,
            ILogger<SearchService> logger)
        {
            _invertedIndex = invertedIndex;
            _documentRepository = documentRepository;
            _textProcessor = textProcessor;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }
        
        public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 10)
        {
            _logger.LogInformation("Searching for query: '{Query}', max results: {MaxResults}", query, maxResults);
            
            var normalizedTerms = _textProcessor.Normalize(query);
            
            if (normalizedTerms.Count == 0)
            {
                _logger.LogWarning("Search query has no valid terms after normalization");
                return new List<SearchResult>();
            }
            
            var searchQuery = new SearchQuery(query, normalizedTerms);
            
            var documentScores = new Dictionary<Guid, double>();
            var termPostings = new Dictionary<string, IReadOnlyList<Posting>>();
            foreach (var term in searchQuery.NormalizedTerms)
            {
                if (_invertedIndex.ContainsTerm(term))
                {
                    var postings = _invertedIndex.GetPostings(term);
                    termPostings[term] = postings;
                    var documentFrequency = postings.Count;
                    double idf = 1.0;
                    
                    var allDocs = await _documentRepository.GetAll();
                    var totalDocuments = allDocs.Count();
                    
                    if (documentFrequency > 0 && totalDocuments > 0)
                    {
                        idf = Math.Log10((double)totalDocuments / documentFrequency);
                    }
                    
                    foreach (var posting in postings)
                    {
                        if (!documentScores.ContainsKey(posting.DocumentId))
                        {
                            documentScores[posting.DocumentId] = 0;
                        }
                        
                        double tf = posting.TermFrequency;
                        double tfIdfScore = tf * idf;
                        
                        documentScores[posting.DocumentId] += tfIdfScore;
                    }
                }
            }
            
            if (searchQuery.NormalizedTerms.Count > 1)
            {
                await ApplyPhraseMatchBoost(documentScores, termPostings, searchQuery.NormalizedTerms);
            }
            
            var topDocumentIds = documentScores
                .OrderByDescending(kv => kv.Value)
                .Take(maxResults)
                .Select(kv => kv.Key);
                
            var documents = await _documentRepository.GetByIds(topDocumentIds);
            
            await ApplyFilenameBoost(documentScores, documents, searchQuery.NormalizedTerms);
            
            topDocumentIds = documentScores
                .OrderByDescending(kv => kv.Value)
                .Take(maxResults)
                .Select(kv => kv.Key);
                
            documents = await _documentRepository.GetByIds(topDocumentIds);
            
            var results = new List<SearchResult>();
            foreach (var document in documents)
            {
                var score = documentScores[document.DocumentId];
                var snippet = await GenerateSnippetAsync(document.DocumentId, searchQuery.NormalizedTerms.First());
                
                results.Add(new SearchResult(
                    document.DocumentId,
                    document.FileName,
                    document.FilePath,
                    score,
                    snippet));
            }
            
            return results.OrderByDescending(r => r.Score).ToList();
        }
        
        private async Task ApplyPhraseMatchBoost(
            Dictionary<Guid, double> documentScores, 
            Dictionary<string, IReadOnlyList<Posting>> termPostings, 
            IReadOnlyList<string> queryTerms)
        {
            var docsWithAllTerms = documentScores.Keys
                .Where(docId => queryTerms.All(term => 
                    termPostings.ContainsKey(term) && 
                    termPostings[term].Any(p => p.DocumentId == docId)))
                .ToList();
                
            foreach (var docId in docsWithAllTerms)
            {
                for (int i = 0; i < queryTerms.Count - 1; i++)
                {
                    var term1 = queryTerms[i];
                    var term2 = queryTerms[i + 1];
                    
                    var postings1 = termPostings[term1].FirstOrDefault(p => p.DocumentId == docId);
                    var postings2 = termPostings[term2].FirstOrDefault(p => p.DocumentId == docId);
                    
                    if (postings1 != null && postings2 != null)
                    {
                        bool hasConsecutiveTerms = false;
                        
                        foreach (var pos1 in postings1.Positions)
                        {
                            if (postings2.Positions.Contains(pos1 + 1))
                            {
                                hasConsecutiveTerms = true;
                                break;
                            }
                        }
                        
                        if (hasConsecutiveTerms)
                            break;
                    }
                }
                
                if (hasConsecutiveTerms)
                {
                    documentScores[docId] *= 1.5;
                }
            }
        }
        
        private async Task ApplyFilenameBoost(
            Dictionary<Guid, double> documentScores, 
            IEnumerable<Document> documents, 
            IReadOnlyList<string> queryTerms)
        {
            foreach (var document in documents)
            {
                if (documentScores.ContainsKey(document.DocumentId))
                {
                    var normalizedFilename = document.FileName.ToLowerInvariant();
                    bool termInFilename = queryTerms.Any(term => normalizedFilename.Contains(term));
                    
                    if (termInFilename)
                    {
                        documentScores[document.DocumentId] *= 1.3;
                    }
                }
            }
        }
        
        public async Task<string> GenerateSnippetAsync(Guid documentId, string searchTerm)
        {
            try
            {
                var document = await _documentRepository.GetById(documentId);
                if (document == null)
                {
                    return string.Empty;
                }
                
                using var fileStream = await _fileStorageService.GetFileAsync(document.FilePath);
                var content = await new StreamReader(fileStream).ReadToEndAsync();
                
                var normalizedContent = content.ToLowerInvariant();
                var normalizedTerm = searchTerm.ToLowerInvariant();
                
                int termIndex = normalizedContent.IndexOf(normalizedTerm);
                if (termIndex == -1)
                {
                    return "...";
                }
                
                int snippetLength = 200;
                int snippetStart = Math.Max(0, termIndex - snippetLength);
                int snippetEnd = Math.Min(content.Length, termIndex + searchTerm.Length + snippetLength);
                
                string snippet = content.Substring(snippetStart, snippetEnd - snippetStart);
                
                if (snippetStart > 0)
                    snippet = "..." + snippet;
                    
                if (snippetEnd < content.Length)
                    snippet = snippet + "...";
                    
                string highlighted = System.Text.RegularExpressions.Regex.Replace(
                    snippet, 
                    searchTerm, 
                    "<b>$&</b>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                return highlighted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating snippet for document {DocumentId}", documentId);
                return "...";
            }
        }
    }
}

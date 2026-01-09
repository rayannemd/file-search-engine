using System;
using System.Collections.Generic;
using System.Linq;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Domain.Interfaces;
using FileSearchEngine.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Infrastructure.Indexing
{
    public class InMemoryInvertedIndex : IInvertedIndex
    {
        private readonly Dictionary<string, List<Posting>> _index = new Dictionary<string, List<Posting>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Term> _terms = new Dictionary<string, Term>(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<InMemoryInvertedIndex> _logger;
        
        public InMemoryInvertedIndex(ILogger<InMemoryInvertedIndex> logger)
        {
            _logger = logger;
        }
        
        public void AddTerm(string termValue, Guid documentId, int position)
        {
            var normalizedValue = termValue.ToLowerInvariant();
            
            if (!_terms.TryGetValue(normalizedValue, out var term))
            {
                term = new Term(termValue, normalizedValue);
                _terms[normalizedValue] = term;
            }
            
            if (!_index.TryGetValue(normalizedValue, out var postings))
            {
                postings = new List<Posting>();
                _index[normalizedValue] = postings;
            }
            
            var posting = postings.FirstOrDefault(p => p.DocumentId == documentId);
            if (posting == null)
            {
                posting = new Posting(documentId);
                postings.Add(posting);
                term.IncrementDocumentFrequency();
            }
            
            posting.AddOccurrence(position);
        }
        
        public IReadOnlyList<Posting> GetPostings(string normalizedTerm)
        {
            if (_index.TryGetValue(normalizedTerm.ToLowerInvariant(), out var postings))
            {
                return postings.AsReadOnly();
            }
            
            return new List<Posting>();
        }
        
        public bool ContainsTerm(string normalizedTerm)
        {
            return _index.ContainsKey(normalizedTerm.ToLowerInvariant());
        }
        
        public void BuildIndex()
        {
            _logger.LogInformation("Index contains {TermCount} unique terms and {PostingsCount} postings", 
                _index.Count, _index.Values.Sum(p => p.Count));
        }
        
        public void Clear()
        {
            _logger.LogWarning("Clearing inverted index");
            _index.Clear();
            _terms.Clear();
        }
    }
}

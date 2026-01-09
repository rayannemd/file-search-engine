using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Domain.Interfaces;
using FileSearchEngine.Infrastructure.Repositories.Interfaces;
using FileSearchEngine.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Services.Implementations
{
    public class IndexingService : IIndexingService
    {
        private readonly IInvertedIndex _invertedIndex;
        private readonly ITextProcessor _textProcessor;
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<IndexingService> _logger;
        private readonly ConcurrentQueue<Document> _indexingQueue = new ConcurrentQueue<Document>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _processingTask;
        
        public IndexingService(
            IInvertedIndex invertedIndex,
            ITextProcessor textProcessor,
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            ILogger<IndexingService> logger)
        {
            _invertedIndex = invertedIndex;
            _textProcessor = textProcessor;
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _logger = logger;
            
            _processingTask = Task.Run(ProcessIndexingQueueAsync);
        }
        
        public async Task IndexDocumentAsync(Document document, Stream contentStream)
        {
            _logger.LogInformation("Indexing document: {FileName} ({DocumentId})", 
                document.FileName, document.DocumentId);
            
            try
            {
                var text = _textProcessor.ExtractText(contentStream, document.FileExtension);
                _logger.LogDebug("Extracted {TextLength} characters from document", text?.Length ?? 0);
                
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Extracted text is empty for document {DocumentId}", document.DocumentId);
                    return;
                }
                
                var tokens = _textProcessor.TokenizeWithPositions(text);
                _logger.LogDebug("Document tokenized into {TokenCount} tokens", tokens.Count);
                
                foreach (var (term, position) in tokens)
                {
                    _invertedIndex.AddTerm(term, document.DocumentId, position);
                }
                
                var filenameWithoutExtension = Path.GetFileNameWithoutExtension(document.FileName);
                var filenameTokens = _textProcessor.Normalize(filenameWithoutExtension);
                
                int filenamePos = -100;
                foreach (var term in filenameTokens)
                {
                    _invertedIndex.AddTerm(term, document.DocumentId, filenamePos++);
                }
                
                _logger.LogInformation("Successfully indexed document {DocumentId}", document.DocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document {DocumentId}", document.DocumentId);
                throw;
            }
        }
        
        public void QueueDocumentForIndexing(Document document)
        {
            _indexingQueue.Enqueue(document);
            _signal.Release();
            _logger.LogInformation("Document {DocumentId} queued for indexing", document.DocumentId);
        }
        
        public async Task RebuildIndexAsync()
        {
            _logger.LogInformation("Rebuilding index");
            
            try
            {
                _invertedIndex.Clear();
                
                var documents = await _documentRepository.GetAllAsync();
                
                foreach (var document in documents)
                {
                    try
                    {
                        using var stream = await _fileStorageService.GetFileAsync(document.FilePath);
                        await IndexDocumentAsync(document, stream);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reindexing document {DocumentId}", document.DocumentId);
                    }
                }
                
                _logger.LogInformation("Index rebuild completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rebuilding index");
                throw;
            }
        }
        
        private async Task ProcessIndexingQueueAsync()
        {
            _logger.LogInformation("Background indexing service started");
            
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(_cancellationTokenSource.Token);
                    
                    if (_indexingQueue.TryDequeue(out var document))
                    {
                        _logger.LogInformation("Processing document {DocumentId} from queue", document.DocumentId);
                        
                        try
                        {
                            using var stream = await _fileStorageService.GetFileAsync(document.FilePath);
                            await IndexDocumentAsync(document, stream);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error indexing document {DocumentId} from queue", document.DocumentId);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background indexing service");
                    await Task.Delay(1000);
                }
            }
            
            _logger.LogInformation("Background indexing service stopped");
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _processingTask.Wait(5000);
            _cancellationTokenSource.Dispose();
        }
    }
}

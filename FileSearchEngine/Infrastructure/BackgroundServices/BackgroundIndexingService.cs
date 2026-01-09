using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Domain.Interfaces;
using FileSearchEngine.Infrastructure.Repositories.Interfaces;
using FileSearchEngine.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Infrastructure.BackgroundServices
{
    public class BackgroundIndexingService : BackgroundService
    {
        private readonly IInvertedIndex _invertedIndex;
        private readonly ITextProcessor _textProcessor;
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<BackgroundIndexingService> _logger;
        private readonly ConcurrentQueue<Document> _indexingQueue = new ConcurrentQueue<Document>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        
        public BackgroundIndexingService(
            IInvertedIndex invertedIndex,
            ITextProcessor textProcessor,
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            ILogger<BackgroundIndexingService> logger)
        {
            _invertedIndex = invertedIndex;
            _textProcessor = textProcessor;
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }
        
        public void QueueDocumentForIndexing(Document document)
        {
            _indexingQueue.Enqueue(document);
            _signal.Release();
            _logger.LogInformation("Document queued for background indexing: {DocumentId}", document.DocumentId);
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background indexing service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _signal.WaitAsync(stoppingToken);
                    
                    if (_indexingQueue.TryDequeue(out var document))
                    {
                        _logger.LogInformation("Processing document {DocumentId} from queue", document.DocumentId);
                        
                        try
                        {
                            using var stream = await _fileStorageService.GetFileAsync(document.FilePath);
                            
                            var text = _textProcessor.ExtractText(stream, document.FileExtension);
                            
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var tokens = _textProcessor.TokenizeWithPositions(text);
                                
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
                            else
                            {
                                _logger.LogWarning("Extracted text is empty for document {DocumentId}", document.DocumentId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error indexing document {DocumentId}", document.DocumentId);
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
                    await Task.Delay(1000, stoppingToken);
                }
            }
            
            _logger.LogInformation("Background indexing service stopped");
        }
    }
}

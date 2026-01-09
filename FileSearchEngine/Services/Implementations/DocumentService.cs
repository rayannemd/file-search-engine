using System;
using System.IO;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Infrastructure.Repositories.Interfaces;
using FileSearchEngine.Services.Interfaces;
using FileSearchEngine.WebApi.Models;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IIndexingService _indexingService;
        private readonly ILogger<DocumentService> _logger;
        
        public DocumentService(
            IDocumentRepository documentRepository,
            IFileStorageService fileStorageService,
            IIndexingService indexingService,
            ILogger<DocumentService> logger)
        {
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _indexingService = indexingService;
            _logger = logger;
        }
        
        public async Task<Document> GetByIdAsync(Guid id)
        {
            var document = await _documentRepository.GetById(id);
            if (document == null)
            {
                _logger.LogWarning("Document with ID {DocumentId} not found", id);
                throw new ArgumentException("Document not found", nameof(id));
            }
            
            return document;
        }
        
        public async Task<UploadResultModel> UploadAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("Processing file upload: {FileName}", fileName);
                
                var hash = await ComputeHashAsync(fileStream);
                fileStream.Position = 0;
                
                if (await _documentRepository.ExistsByName(fileName))
                {
                    _logger.LogInformation("File {FileName} already exists in the index", fileName);
                    return new UploadResultModel
                    {
                        IndexedSuccessfully = false,
                        Message = "File already exists in the index"
                    };
                }
                
                var fileExtension = Path.GetExtension(fileName);
                var filePath = await _fileStorageService.StoreFileAsync(fileStream, fileName);
                fileStream.Position = 0;
                
                var document = new Document(
                    Path.GetFileName(fileName),
                    fileExtension,
                    filePath,
                    fileStream.Length,
                    hash);
                    
                await _documentRepository.Save(document);
                
                _indexingService.QueueDocumentForIndexing(document);
                
                _logger.LogInformation("File {FileName} uploaded successfully with ID {DocumentId}", fileName, document.DocumentId);
                
                return new UploadResultModel
                {
                    DocumentId = document.DocumentId,
                    FileName = document.FileName,
                    IndexedSuccessfully = true,
                    Message = "File uploaded and queued for indexing"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload: {FileName}", fileName);
                
                return new UploadResultModel
                {
                    IndexedSuccessfully = false,
                    Message = $"Error processing file: {ex.Message}"
                };
            }
        }
        
        public async Task<Stream> DownloadAsync(Guid id)
        {
            var document = await GetByIdAsync(id);
            
            try
            {
                _logger.LogInformation("Downloading file: {FileName} ({DocumentId})", document.FileName, document.DocumentId);
                return await _fileStorageService.GetFileAsync(document.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName} ({DocumentId})", document.FileName, document.DocumentId);
                throw;
            }
        }
        
        private async Task<string> ComputeHashAsync(Stream stream)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = await md5.ComputeHashAsync(stream);
            
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}

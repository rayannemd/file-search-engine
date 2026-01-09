using System;
using System.IO;
using System.Threading.Tasks;
using FileSearchEngine.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _storageBasePath;
        private readonly ILogger<FileStorageService> _logger;
        
        public FileStorageService(string storageBasePath, ILogger<FileStorageService> logger)
        {
            _storageBasePath = storageBasePath;
            _logger = logger;
            
            Directory.CreateDirectory(_storageBasePath);
        }
        
        public async Task<string> StoreFileAsync(Stream fileStream, string fileName)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(_storageBasePath, uniqueFileName);
            
            _logger.LogInformation("Storing file {FileName} to {FilePath}", fileName, filePath);
            
            try
            {
                using var fileStream2 = new FileStream(filePath, FileMode.Create);
                await fileStream.CopyToAsync(fileStream2);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing file {FileName}", fileName);
                throw;
            }
        }
        
        public Task<Stream> GetFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                throw new FileNotFoundException("File not found", filePath);
            }
            
            try
            {
                Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return Task.FromResult(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FilePath}", filePath);
                throw;
            }
        }
        
        public Task DeleteFileAsync(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted file {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
                    throw;
                }
            }
            
            return Task.CompletedTask;
        }
    }
}

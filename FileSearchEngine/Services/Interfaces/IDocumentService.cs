using System;
using System.IO;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.WebApi.Models;

namespace FileSearchEngine.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> GetByIdAsync(Guid id);
        Task<UploadResultModel> UploadAsync(Stream fileStream, string fileName);
        Task<Stream> DownloadAsync(Guid id);
    }
}

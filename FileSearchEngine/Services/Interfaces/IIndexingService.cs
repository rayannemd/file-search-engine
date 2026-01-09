using System.IO;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;

namespace FileSearchEngine.Services.Interfaces
{
    public interface IIndexingService
    {
        Task IndexDocumentAsync(Document document, Stream contentStream);
        void QueueDocumentForIndexing(Document document);
        Task RebuildIndexAsync();
    }
}

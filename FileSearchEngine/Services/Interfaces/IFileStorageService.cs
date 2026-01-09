using System.IO;
using System.Threading.Tasks;

namespace FileSearchEngine.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> StoreFileAsync(Stream fileStream, string fileName);
        Task<Stream> GetFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;

namespace FileSearchEngine.Infrastructure.Repositories.Interfaces
{
    public interface IDocumentRepository
    {
        Task<Document> GetById(Guid id);
        Task<IEnumerable<Document>> GetByIds(IEnumerable<Guid> ids);
        Task<Guid> Save(Document document);
        Task<IEnumerable<Document>> GetAll();
        Task<bool> ExistsByHash(string hash);
        Task<bool> ExistsByName(string fileName);
    }
}

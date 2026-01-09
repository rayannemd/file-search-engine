using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileSearchEngine.Domain.Entities;
using FileSearchEngine.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Infrastructure.Repositories.Implementations
{
    public class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly ConcurrentDictionary<Guid, Document> _documents = new ConcurrentDictionary<Guid, Document>();
        private readonly ConcurrentDictionary<string, Document> _documentsByHash = new ConcurrentDictionary<string, Document>();
        private readonly ILogger<InMemoryDocumentRepository> _logger;
        private readonly object _lock = new object();
        
        public InMemoryDocumentRepository(ILogger<InMemoryDocumentRepository> logger)
        {
            _logger = logger;
        }
        
        public Task<Document> GetById(Guid id)
        {
            _logger.LogDebug("Getting document by ID: {DocumentId}", id);
            
            if (_documents.TryGetValue(id, out var document))
            {
                return Task.FromResult(document);
            }
            
            _logger.LogWarning("Document not found with ID: {DocumentId}", id);
            return Task.FromResult<Document>(null);
        }
        
        public Task<IEnumerable<Document>> GetByIds(IEnumerable<Guid> ids)
        {
            _logger.LogDebug("Getting documents by IDs");
            
            var results = ids
                .Where(id => _documents.ContainsKey(id))
                .Select(id => _documents[id]);
                
            return Task.FromResult(results);
        }
        
        public Task<Guid> Save(Document document)
        {
            _logger.LogInformation("Saving document: {FileName} ({DocumentId})", document.FileName, document.DocumentId);
            
            _documents.AddOrUpdate(document.DocumentId, document, (key, oldValue) => document);
            _documentsByHash.AddOrUpdate(document.Hash, document, (key, oldValue) => document);
            
            return Task.FromResult(document.DocumentId);
        }
        
        public Task<IEnumerable<Document>> GetAll()
        {
            _logger.LogDebug("Getting all documents, count: {Count}", _documents.Count);
            return Task.FromResult(_documents.Values.AsEnumerable());
        }
        
        public Task<bool> ExistsByHash(string hash)
        {
            var exists = _documentsByHash.ContainsKey(hash);
            _logger.LogDebug("Checking if document exists by hash: {Hash}, Result: {Exists}", hash, exists);
            return Task.FromResult(exists);
        }

        public Task<bool> ExistsByName(string fileName)
        {
            var exists = _documents.Any(d => d.Value.FileName == fileName);
            _logger.LogDebug("Checking if document exists by name: {FileName}, Result: {Exists}", fileName, exists);
            return Task.FromResult(exists);
        }
    }
}

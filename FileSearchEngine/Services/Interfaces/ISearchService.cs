using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FileSearchEngine.Domain.ValueObjects;

namespace FileSearchEngine.Services.Interfaces
{
    public interface ISearchService
    {
        Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 10);
        Task<string> GenerateSnippetAsync(Guid documentId, string searchTerm);
    }
}

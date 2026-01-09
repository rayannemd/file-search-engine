using System;
using System.Collections.Generic;
using FileSearchEngine.Domain.Entities;

namespace FileSearchEngine.Domain.Interfaces
{
    public interface IInvertedIndex
    {
        void AddTerm(string term, Guid documentId, int position);
        IReadOnlyList<Posting> GetPostings(string normalizedTerm);
        bool ContainsTerm(string normalizedTerm);
        void BuildIndex();
        void Clear();
    }
}

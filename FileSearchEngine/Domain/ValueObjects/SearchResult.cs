using System;

namespace FileSearchEngine.Domain.ValueObjects
{
    public class SearchResult
    {
        public Guid DocumentId { get; private set; }
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public double Score { get; private set; }
        public string Snippet { get; private set; }
        
        public SearchResult(Guid documentId, string fileName, string filePath, double score, string snippet)
        {
            DocumentId = documentId;
            FileName = fileName;
            FilePath = filePath;
            Score = score;
            Snippet = snippet;
        }
    }
}

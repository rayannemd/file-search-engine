using System;

namespace FileSearchEngine.WebApi.Models
{
    public class SearchResultModel
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public double Score { get; set; }
        public string Snippet { get; set; }
    }
}

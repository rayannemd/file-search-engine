using System;

namespace FileSearchEngine.WebApi.Models
{
    public class UploadResultModel
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; }
        public bool IndexedSuccessfully { get; set; }
        public string Message { get; set; }
    }
}

using System;

namespace FileSearchEngine.Domain.Entities
{
    public class Document
    {
        public Guid DocumentId { get; private set; }
        public string FileName { get; private set; }
        public string FileExtension { get; private set; }
        public string FilePath { get; private set; }
        public DateTime UploadedAt { get; private set; }
        public long ContentLength { get; private set; }
        public string Hash { get; private set; }
        
        public Document(string fileName, string fileExtension, string filePath, long contentLength, string hash)
        {
            DocumentId = Guid.NewGuid();
            FileName = fileName;
            FileExtension = fileExtension;
            FilePath = filePath;
            UploadedAt = DateTime.UtcNow;
            ContentLength = contentLength;
            Hash = hash;
        }
        
        public Document(Guid documentId, string fileName, string fileExtension, string filePath, 
                       DateTime uploadedAt, long contentLength, string hash)
        {
            DocumentId = documentId;
            FileName = fileName;
            FileExtension = fileExtension;
            FilePath = filePath;
            UploadedAt = uploadedAt;
            ContentLength = contentLength;
            Hash = hash;
        }
    }
}

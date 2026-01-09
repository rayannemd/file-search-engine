using System;
using System.Collections.Generic;

namespace FileSearchEngine.Domain.Entities
{
    public class Posting
    {
        public Guid DocumentId { get; private set; }
        public int TermFrequency { get; private set; }
        public List<int> Positions { get; private set; }
        
        public Posting(Guid documentId)
        {
            DocumentId = documentId;
            TermFrequency = 0;
            Positions = new List<int>();
        }
        
        public void AddOccurrence(int position)
        {
            TermFrequency++;
            Positions.Add(position);
        }
    }
}

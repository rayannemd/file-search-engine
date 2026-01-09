using System.Collections.Generic;

namespace FileSearchEngine.Domain.ValueObjects
{
    public class SearchQuery
    {
        public string RawQuery { get; private set; }
        public IReadOnlyList<string> NormalizedTerms { get; private set; }
        
        public SearchQuery(string rawQuery, IReadOnlyList<string> normalizedTerms)
        {
            RawQuery = rawQuery;
            NormalizedTerms = normalizedTerms;
        }
    }
}

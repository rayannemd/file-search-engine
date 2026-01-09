namespace FileSearchEngine.Domain.ValueObjects
{
    public class Term
    {
        public string Value { get; private set; }
        public string NormalizedValue { get; private set; }
        public int DocumentFrequency { get; private set; }
        
        public Term(string value, string normalizedValue)
        {
            Value = value;
            NormalizedValue = normalizedValue;
            DocumentFrequency = 0;
        }
        
        public void IncrementDocumentFrequency()
        {
            DocumentFrequency++;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is not Term other)
                return false;
                
            return NormalizedValue == other.NormalizedValue;
        }
        
        public override int GetHashCode()
        {
            return NormalizedValue.GetHashCode();
        }
    }
}

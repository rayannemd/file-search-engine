using System.Collections.Generic;
using System.IO;

namespace FileSearchEngine.Domain.Interfaces
{
    public interface ITextProcessor
    {
        string ExtractText(Stream fileStream, string fileExtension);
        IReadOnlyList<string> Normalize(string text);
        IReadOnlyList<(string Term, int Position)> TokenizeWithPositions(string text);
    }
}

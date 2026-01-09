using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using FileSearchEngine.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileSearchEngine.Infrastructure.TextProcessing
{
    public class TextProcessor : ITextProcessor
    {
        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "a", "an", "the", "and", "or", "but", "is", "are", "was", "were", 
            "in", "on", "at", "to", "for", "with", "by", "about", "of", "as",
            "de", "a", "o", "que", "e", "do", "da", "em", "um", "para", "é", "com", "não",
            "uma", "os", "no", "se", "na", "por", "mais", "as", "dos", "como"
        };
        
        private readonly ILogger<TextProcessor> _logger;
        
        public TextProcessor(ILogger<TextProcessor> logger)
        {
            _logger = logger;
        }

        public string ExtractText(Stream fileStream, string fileExtension)
        {
            _logger.LogInformation("Extracting text from file with extension {FileExtension}", fileExtension);
            
            try
            {
                return fileExtension.ToLower() switch
                {
                    ".txt" => ExtractFromTxt(fileStream),
                    ".pdf" => ExtractFromPdf(fileStream),
                    ".docx" => ExtractFromDocx(fileStream),
                    _ => throw new NotSupportedException($"File extension {fileExtension} not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from file with extension {FileExtension}", fileExtension);
                throw;
            }
        }

        public IReadOnlyList<string> Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }

            var lowercase = text.ToLowerInvariant();
            
            var normalized = RemoveDiacritics(lowercase);
            
            var noPunctuation = Regex.Replace(normalized, @"[^\w\s]", " ");
            
            var tokens = noPunctuation.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            return tokens.Where(t => !StopWords.Contains(t)).ToList();
        }

        public IReadOnlyList<(string Term, int Position)> TokenizeWithPositions(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<(string, int)>();

            var lowercase = text.ToLowerInvariant();
            
            var normalized = RemoveDiacritics(lowercase);
            
            var noPunctuation = Regex.Replace(normalized, @"[^\w\s]", " ");
            
            var tokens = noPunctuation.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            var result = new List<(string, int)>();
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!StopWords.Contains(tokens[i]))
                {
                    result.Add((tokens[i], i));
                }
            }
            
            return result;
        }

        private string ExtractFromTxt(Stream fileStream)
        {
            using var reader = new StreamReader(fileStream, leaveOpen: true);
            return reader.ReadToEnd();
        }

        private string ExtractFromPdf(Stream fileStream)
        {
            _logger.LogWarning("PDF extraction is not fully implemented - returning placeholder");
            return "[PDF content extraction not implemented]";
        }

        private string ExtractFromDocx(Stream fileStream)
        {
            _logger.LogWarning("DOCX extraction is not fully implemented - returning placeholder");
            return "[DOCX content extraction not implemented]";
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}

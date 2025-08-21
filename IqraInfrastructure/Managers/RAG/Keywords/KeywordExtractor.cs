using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.RAG.Keywords
{
    public class KeywordExtractor
    {
        private static readonly Regex WordSplitterRegex = new Regex(@"\W+", RegexOptions.Compiled);

        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it", "its",
            "of", "on", "that", "the", "to", "was", "were", "will", "with", "what", "which", "who", "when", "where", "why", "how"
            // This list can be expanded
        };

        public List<string> Extract(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            var lowerText = text.ToLowerInvariant();

            var tokens = WordSplitterRegex.Split(lowerText)
                .Where(token => !string.IsNullOrWhiteSpace(token) && !StopWords.Contains(token))
                .Distinct()
                .ToList();

            return tokens;
        }
    }
}

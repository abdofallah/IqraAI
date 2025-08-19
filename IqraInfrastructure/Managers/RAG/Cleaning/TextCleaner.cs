using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Managers.RAG.Cleaning
{
    public static class TextCleaner
    {
        // Regex to find URLs
        private static readonly Regex UrlRegex = new Regex(@"https?:\/\/[^\s/$.?#].[^\s]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex to find consecutive whitespace characters (spaces, tabs, etc.)
        private static readonly Regex ConsecutiveSpacesRegex = new Regex(@"[ \t\f\r\x20\u00a0\u1680\u180e\u2000-\u200a\u202f\u205f\u3000]{2,}", RegexOptions.Compiled);

        // Regex to find three or more consecutive newline characters
        private static readonly Regex ConsecutiveNewlinesRegex = new Regex(@"\n{3,}", RegexOptions.Compiled);

        public static string Clean(string text, TextPreProcessingRules rules)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var cleanedText = text;

            if (rules.DeleteUrls)
            {
                cleanedText = UrlRegex.Replace(cleanedText, string.Empty);
            }

            if (rules.ReplaceConsecutive)
            {
                cleanedText = ConsecutiveNewlinesRegex.Replace(cleanedText, "\n\n");
                cleanedText = ConsecutiveSpacesRegex.Replace(cleanedText, " ");
            }

            return cleanedText.Trim();
        }
    }
}

using System.Text;

namespace IqraCore.Utilities
{
    public static class HttpHeaderTextHelper
    {
        private const string NameSymbols = "!#$%&'*+-.^_`|~";

        public static string CleanValue(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var cleaned = new StringBuilder(value.Length);
            bool previousWasLineBreak = false;

            foreach (char character in value)
            {
                if (IsLineBreak(character))
                {
                    if (!previousWasLineBreak) cleaned.Append(' ');
                    previousWasLineBreak = true;
                    continue;
                }

                previousWasLineBreak = false;

                if (character == '\t')
                {
                    cleaned.Append(' ');
                }
                else if (!IsRemovedControl(character))
                {
                    cleaned.Append(character);
                }
            }

            return cleaned.ToString().Trim();
        }

        public static bool IsValidName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            foreach (char character in name)
            {
                bool isLetterOrDigit = (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9');
                if (!isLetterOrDigit && NameSymbols.IndexOf(character) < 0) return false;
            }

            return true;
        }

        private static bool IsLineBreak(char character)
        {
            return character == '\r' || character == '\n' || character == '\u000B' || character == '\u000C' || character == '\u0085' || character == '\u2028' || character == '\u2029';
        }

        private static bool IsRemovedControl(char character)
        {
            return (character <= '\u0008') || (character >= '\u000E' && character <= '\u001F') || character == '\u007F';
        }
    }
}

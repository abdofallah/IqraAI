using System.Text;

namespace IqraCore.Utilities
{
    public static class AIResponseHelper
    {
        public static (List<string>, StringBuilder?) SeparateTextIntoSectionsNew(string text, ref int charactersConverted)
        {
            var sections = new List<string>();
            var remainingSection = new StringBuilder();

            List<int> seperatorIndexes = new List<int>();
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];

                if (IsSectionSeparator(character))
                {
                    seperatorIndexes.Add(i);
                }
            }

            if (seperatorIndexes.Count == 0)
            {
                remainingSection.Append(text);
                return (sections, remainingSection);
            }

            int lastSeperatorIndex = -1;
            bool useLastSeperator = charactersConverted > 27; // make this dynamic

            if (useLastSeperator)
            {
                if (seperatorIndexes.Count >= 2) // make this dynamic
                {
                    lastSeperatorIndex = seperatorIndexes[seperatorIndexes.Count - 1];
                }
                else
                {
                    remainingSection.Append(text);
                    return (sections, remainingSection);
                }
            }
            else
            {
                lastSeperatorIndex = seperatorIndexes[0];
            }

            string textTillLastSeperator = text.Substring(0, lastSeperatorIndex + 1);

            if (textTillLastSeperator.EndsWith("Muscat,")) // make this dynamic
            {
                if (useLastSeperator)
                {
                    lastSeperatorIndex = seperatorIndexes[seperatorIndexes.Count - 2];
                }
                else
                {
                    remainingSection.Append(text);
                    return (sections, remainingSection);
                }
            }

            string remainingTextAfterSeperator = text.Substring(lastSeperatorIndex + 1);

            sections.Add(textTillLastSeperator);
            remainingSection.Append(remainingTextAfterSeperator);

            if (useLastSeperator)
            {
                charactersConverted = 0;
            }

            return (sections, remainingSection);
        }

        public static (List<string>, StringBuilder?) SeparateTextIntoSections(string text, ref int charactersConverted)
        {
            var sections = new List<string>();
            var remainingSection = new StringBuilder();

            bool useLastSeperator = charactersConverted > 27; // make this dynamic

            int seperatorCount = 0;
            int lastSeperatorIndex = -1;

            int characterIndex = useLastSeperator ? (text.Length - 1) : 0;
            while (true)
            {
                char character = text[characterIndex];

                if (useLastSeperator)
                {
                    characterIndex -= 1;

                    if (characterIndex < 0)
                    {
                        remainingSection.Append(text);
                        break;
                    }

                    if (IsSectionSeparator(character))
                    {
                        seperatorCount++;

                        if (lastSeperatorIndex == -1)
                        {
                            lastSeperatorIndex = characterIndex;
                        }

                        if (seperatorCount >= 2)
                        {
                            string textTillLastSeperator = text.Substring(0, lastSeperatorIndex + 2);
                            string remainingTextAfterSeperator = text.Substring(lastSeperatorIndex + 2);

                            sections.Add(textTillLastSeperator);
                            remainingSection.Append(remainingTextAfterSeperator);

                            charactersConverted = 0;
                            break;
                        }
                    }
                }
                else
                {
                    remainingSection.Append(character);
                    if (IsSectionSeparator(character))
                    {
                        var section = remainingSection.ToString().Trim();
                        if (!string.IsNullOrEmpty(section))
                        {
                            sections.Add(section);
                        }
                        remainingSection.Clear();
                    }

                    characterIndex += 1;

                    if (characterIndex >= text.Length)
                    {
                        break;
                    }
                }
            }

            return (sections, remainingSection);
        }

        public static bool IsSectionSeparator(char character)
        {
            return character == '.' || character == '!' || character == '?' || character == ',';
        }
    }
}

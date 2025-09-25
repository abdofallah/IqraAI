using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Interfaces.RAG;

namespace IqraInfrastructure.Managers.RAG.Splitters
{
    public enum SplitterType { Parent, Child }

    public class TextSplitterFactory
    {
        public ITextSplitter Create(BusinessAppKnowledgeBaseConfigurationChunking config, SplitterType? type = null)
        {
            switch (config.Type)
            {
                case KnowledgeBaseChunkingType.General:
                {
                    var generalConfig = (BusinessAppKnowledgeBaseConfigurationGeneralChunking)config;
                    return new RecursiveCharacterTextSplitter(generalConfig.MaxLength, generalConfig.Overlap, ReplaceTextToEscapeSequence(generalConfig.Delimiter));
                }

                case KnowledgeBaseChunkingType.ParentChild:
                {
                    var parentChildConfig = (BusinessAppKnowledgeBaseConfigurationParentChildChunking)config;
                    if (type == SplitterType.Parent)
                    {
                        if (parentChildConfig.Parent.Type != KnowledgeBaseChunkingParentChunkType.Paragraph)
                        {
                            // For FullDoc, the splitter is not used for the parent. We can return a "do-nothing" splitter.
                            return new RecursiveCharacterTextSplitter(int.MaxValue, 0);
                        }
                        return new RecursiveCharacterTextSplitter(parentChildConfig.Parent.MaxLength!.Value, 0, ReplaceTextToEscapeSequence(parentChildConfig.Parent.Delimiter)); // Parents have no overlap
                    }
                    else // Child
                    {
                        return new RecursiveCharacterTextSplitter(parentChildConfig.Child.MaxLength, 0, ReplaceTextToEscapeSequence(parentChildConfig.Child.Delimiter));
                    }
                }

                default:
                    throw new NotSupportedException($"Chunking type '{config.Type}' is not supported by the factory.");
            }
        }

        private string ReplaceTextToEscapeSequence(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                // IMPORTANT: Replace the backslash escape first!
                // This prevents "\\n" from becoming "\n" and then the '\' getting removed.
                .Replace("\\\\", "\\")  // Literal backslash

                // Common Newline and Whitespace Characters (Most important for chunking)
                .Replace("\\n", "\n")  // Newline / Line Feed (LF)
                .Replace("\\r", "\r")  // Carriage Return (CR)
                .Replace("\\t", "\t")  // Horizontal Tab
                .Replace("\\f", "\f")  // Form Feed (often used for page breaks)
                .Replace("\\v", "\v")  // Vertical Tab

                // Other escape sequences (less common as delimiters, but good to have)
                .Replace("\\'", "\'")  // Single quote
                .Replace("\\\"", "\"") // Double quote
                .Replace("\\0", "\0")  // Null character
                .Replace("\\a", "\a")  // Alert (bell)
                .Replace("\\b", "\b"); // Backspace
        }
    }
}

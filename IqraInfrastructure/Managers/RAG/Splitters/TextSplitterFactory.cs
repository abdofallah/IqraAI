using IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking;
using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using IqraCore.Interfaces.RAG;

namespace IqraInfrastructure.Managers.RAG.Splitters
{
    public enum SplitterType { Parent, Child }

    /// <summary>
    /// A factory for creating configured instances of ITextSplitter.
    /// </summary>
    public class TextSplitterFactory
    {
        private static readonly List<string> DefaultSeparators = new List<string> { "\n\n", "\n", ". ", " ", "" };

        public ITextSplitter Create(BusinessAppKnowledgeBaseConfigurationChunking config, SplitterType? type = null)
        {
            switch (config.Type)
            {
                case KnowledgeBaseChunkingType.General:
                {
                    var generalConfig = (BusinessAppKnowledgeBaseConfigurationGeneralChunking)config;
                    var generalSeparators = new List<string> { generalConfig.Delimiter.Replace("\\n", "\n") };
                    generalSeparators.AddRange(DefaultSeparators);
                    return new RecursiveCharacterTextSplitter(generalConfig.MaxLength, generalConfig.Overlap, generalSeparators);
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
                        var parentSeparators = new List<string> { parentChildConfig.Parent.Delimiter!.Replace("\\n", "\n") };
                        parentSeparators.AddRange(DefaultSeparators);
                        return new RecursiveCharacterTextSplitter(parentChildConfig.Parent.MaxLength!.Value, 0, parentSeparators); // Parents have no overlap
                    }
                    else // Child
                    {
                        var childSeparators = new List<string> { parentChildConfig.Child.Delimiter.Replace("\\n", "\n") };
                        childSeparators.AddRange(DefaultSeparators);
                        // Child chunks do not overlap as they are contained within a parent.
                        return new RecursiveCharacterTextSplitter(parentChildConfig.Child.MaxLength, 0, childSeparators);
                    }
                }

                default:
                    throw new NotSupportedException($"Chunking type '{config.Type}' is not supported by the factory.");
            }
        }
    }
}

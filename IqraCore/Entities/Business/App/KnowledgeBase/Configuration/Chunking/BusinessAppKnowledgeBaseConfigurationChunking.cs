using IqraCore.Entities.Business.App.KnowledgeBase.ENUM;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase.Configuration.Chunking
{
    // --- Chunking ---
    [BsonKnownTypes(typeof(BusinessAppKnowledgeBaseConfigurationGeneralChunking), typeof(BusinessAppKnowledgeBaseConfigurationParentChildChunking))]
    public abstract class BusinessAppKnowledgeBaseConfigurationChunking
    {
        public abstract KnowledgeBaseChunkingType Type { get; set; }
    }

    public class BusinessAppKnowledgeBaseConfigurationGeneralChunking : BusinessAppKnowledgeBaseConfigurationChunking
    {
        public override KnowledgeBaseChunkingType Type { get; set; } = KnowledgeBaseChunkingType.General;
        public string Delimiter { get; set; } = "\\n\\n";
        public int MaxLength { get; set; } = 1024;
        public int Overlap { get; set; } = 50;
        public TextPreProcessingRules Preprocess { get; set; } = new();
    }

    public class BusinessAppKnowledgeBaseConfigurationParentChildChunking : BusinessAppKnowledgeBaseConfigurationChunking
    {
        public override KnowledgeBaseChunkingType Type { get; set; } = KnowledgeBaseChunkingType.ParentChild;
        public ParentChunkContextSettings Parent { get; set; } = new();
        public ChildChunkRetrievalSettings Child { get; set; } = new();
        public TextPreProcessingRules Preprocess { get; set; } = new();
    }

    public class ParentChunkContextSettings
    {
        public KnowledgeBaseChunkingParentChunkType Type { get; set; } = KnowledgeBaseChunkingParentChunkType.Paragraph;
        public string? Delimiter { get; set; } = null;
        public int? MaxLength { get; set; } = null;
    }

    public class ChildChunkRetrievalSettings
    {
        public string Delimiter { get; set; } = "\\n";
        public int MaxLength { get; set; } = 512;
    }

    // Common
    public class TextPreProcessingRules
    {
        public bool ReplaceConsecutive { get; set; }
        public bool DeleteUrls { get; set; }
    }
}

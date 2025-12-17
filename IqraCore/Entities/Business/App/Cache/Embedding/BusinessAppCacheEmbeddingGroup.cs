using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheEmbeddingGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppCacheEmbedding>> Embeddings { get; set; } = new Dictionary<string, List<BusinessAppCacheEmbedding>>();

        public List<string> AgentReferences { get; set; } = new List<string>();
        public List<string> AgentAutoCacheReferences { get; set; } = new List<string>();
    }
}

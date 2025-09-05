namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCache
    {
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Audios { get; set; } = new List<string>();
        public BusinessAppAgentAutoCacheAudioSettings AudioCacheSettings { get; set; } = new BusinessAppAgentAutoCacheAudioSettings();

        public List<string> Embeddings { get; set; } = new List<string>();
        public BusinessAppAgentAutoCacheEmbeddingsSettings EmbeddingsCacheSettings { get; set; } = new BusinessAppAgentAutoCacheEmbeddingsSettings();
    }
}

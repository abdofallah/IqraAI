using IqraCore.Interfaces.Embedding;

namespace IqraCore.Entities.Embedding.Providers.GoogleGemini
{
    public class GoogleGeminiEmbeddingServiceConfig : IEmbeddingConfig
    {
        public int ConfigVersion => 1;

        public string Model { get; set; }
        public int VectorDimension { get; set; }
    }
}

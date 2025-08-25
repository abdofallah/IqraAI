using System.Text.Json.Serialization;

namespace IqraCore.Interfaces.Embedding
{
    public class IEmbeddingConfig
    {
        [JsonPropertyOrder(-1)]
        public int ConfigVersion { get; }
    }
}

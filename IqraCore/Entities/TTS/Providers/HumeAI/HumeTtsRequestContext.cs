using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeTtsRequestContext
    {
        [JsonPropertyName("generation_id")]
        public string GenerationId { get; set; }
    }
}
using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Hamsa
{
    public class HamsaTtsApiRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; }

        [JsonPropertyName("dialect")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Dialect { get; set; }

        [JsonPropertyName("mulaw")]
        public bool MuLaw { get; set; } = false;
    }
}

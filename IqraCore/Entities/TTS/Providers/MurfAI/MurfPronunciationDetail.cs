using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.MurfAI
{
    public class MurfPronunciationDetail
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("pronunciation")]
        public string Pronunciation { get; set; }
    }
}

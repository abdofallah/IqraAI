using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Speechify
{
    public class SpeechifyTtsRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("voice_id")]
        public string VoiceId { get; set; } = string.Empty;


        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("audio_format")]
        public string AudioFormat { get; set; } = "wav"; // Request WAV

        [JsonPropertyName("language")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Language { get; set; }

        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpeechifyOptionsRequest? Options { get; set; }
    }
}
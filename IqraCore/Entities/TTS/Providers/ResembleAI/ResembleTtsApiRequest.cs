using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleTtsApiRequest
    {
        [JsonPropertyName("project_uuid")]
        public string ProjectUuid { get; set; }

        [JsonPropertyName("voice_uuid")]
        public string VoiceUuid { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; } // The text to synthesize

        [JsonPropertyName("precision")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Precision { get; set; } // "PCM_32", "PCM_24", "PCM_16", "MULAW". Default PCM_32. We'll request PCM_16 for consistency.

        [JsonPropertyName("sample_rate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SampleRate { get; set; } // 8000, 16000, 22050, 32000, 44100, 48000. Default 22050.
    }
}

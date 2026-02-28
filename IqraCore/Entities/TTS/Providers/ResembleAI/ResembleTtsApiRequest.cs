using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.ResembleAI
{
    public class ResembleTtsApiRequest
    {
        [JsonPropertyName("voice_uuid")]
        public string VoiceUuid { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("project_uuid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ProjectUuid { get; set; }

        [JsonPropertyName("model")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Model { get; set; }

        [JsonPropertyName("precision")]
        public string Precision { get; set; }

        [JsonPropertyName("sample_rate")]
        public int SampleRate { get; set; }

        [JsonPropertyName("use_hd")]
        public bool UseHd { get; set; }

        [JsonPropertyName("output_format")]
        public string OutputFormat { get; set; }
    }
}

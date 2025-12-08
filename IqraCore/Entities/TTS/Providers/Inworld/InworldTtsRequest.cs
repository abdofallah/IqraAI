using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Inworld
{
    public class InworldTtsRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("voiceId")]
        public string VoiceId { get; set; }

        [JsonPropertyName("modelId")]
        public string ModelId { get; set; }

        [JsonPropertyName("audioConfig")]
        public InworldAudioConfig AudioConfig { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("applyTextNormalization")]
        public string? ApplyTextNormalization { get; set; }
    }

    public class InworldAudioConfig
    {
        [JsonPropertyName("audioEncoding")]
        public string AudioEncoding { get; set; }

        [JsonPropertyName("sampleRateHertz")]
        public int SampleRateHertz { get; set; }

        [JsonPropertyName("speakingRate")]
        public double SpeakingRate { get; set; }
    }
}
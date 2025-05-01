using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.HumeAI
{
    public class HumeTtsRequest
    {
        [JsonPropertyName("utterances")]
        public List<HumeUtteranceRequest> Utterances { get; set; } = new();

        // Optional context (not implemented here, for continuation)
        // [JsonPropertyName("context")]
        // public object? Context { get; set; }

        // Optional: Request multiple generations (default 1)
        // [JsonPropertyName("num_generations")]
        // public int? NumGenerations { get; set; }

        // Optional: Prevent utterance splitting (default false)
        // [JsonPropertyName("split_utterances")]
        // public bool? SplitUtterances { get; set; }

        [JsonPropertyName("format")]
        public HumeTtsRequestAudioFormat AudioFormat { get; set; } = new();
    }

    public class HumeTtsRequestAudioFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "pcm";
    }
}

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

        // --- How to specify output format? ---
        // The documentation doesn't explicitly show a format parameter in the main JSON request.
        // It might be inferred or potentially set via headers or another schema object.
        // We will proceed assuming default or WAV/MP3 and handle the response.
        // If PCM can be requested, this model would need updating.
    }
}

using System.Text.Json.Serialization;

namespace IqraCore.Entities.TTS.Providers.Inworld
{
    public class InworldTtsResponse
    {
        [JsonPropertyName("result")]
        public InworldTtsResult? Result { get; set; }

        [JsonPropertyName("error")]
        public InworldTtsError? Error { get; set; }
    }

    public class InworldTtsResult
    {
        [JsonPropertyName("audioContent")]
        public string? AudioContent { get; set; }

        // We can add timestampInfo later if needed, but not required for audio playback
    }

    public class InworldTtsError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        // Details omitted for brevity unless needed for logging
    }
}

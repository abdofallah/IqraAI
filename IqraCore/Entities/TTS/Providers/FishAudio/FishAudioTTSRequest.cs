using MessagePack;

namespace IqraCore.Entities.TTS.Providers.FishAudio
{
    [MessagePackObject]
    public class FishAudioTTSRequest
    {
        [Key("text")]
        public string Text { get; set; } = string.Empty;

        [Key("reference_id")]
        public string? ReferenceId { get; set; } // Use pre-uploaded model ID

        [Key("format")]
        public string Format { get; set; } = "wav"; // Request WAV to get header info

        [Key("normalize")]
        public bool Normalize { get; set; } = true;

        [Key("latency")]
        public string Latency { get; set; } = "normal";

        // Add other parameters like references, chunk_length, mp3_bitrate if needed
        // For simplicity, we'll stick to reference_id and basic settings.
        [Key("references")]
        [IgnoreMember] // Ignore if not using direct audio references
        public object[]? References { get; set; } = null; // Placeholder if needed later
    }
}

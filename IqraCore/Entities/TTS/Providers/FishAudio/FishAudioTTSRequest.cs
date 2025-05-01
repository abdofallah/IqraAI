using MessagePack;

namespace IqraCore.Entities.TTS.Providers.FishAudio
{
    [MessagePackObject]
    public class FishAudioTTSRequest
    {
        [Key("text")]
        public string Text { get; set; } = string.Empty;

        [Key("reference_id")]
        public string? ReferenceId { get; set; }

        [Key("format")]
        public string Format { get; set; } = "pcm";

        [Key("sample_rate")]
        public int SampleRate { get; set; } = 8;

        [Key("normalize")]
        public bool Normalize { get; set; } = true;

        [Key("latency")]
        public string Latency { get; set; } = "balanced";

        [Key("references")]
        [IgnoreMember]
        public object[]? References { get; set; } = null;
    }
}

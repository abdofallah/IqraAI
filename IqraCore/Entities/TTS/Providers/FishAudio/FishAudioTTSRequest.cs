using MessagePack;

namespace IqraCore.Entities.TTS.Providers.FishAudio
{
    [MessagePackObject]
    public class FishAudioProsody
    {
        [Key("speed")]
        public float Speed { get; set; } = 1.0f;

        [Key("volume")]
        public float Volume { get; set; } = 0.0f;
    }

    [MessagePackObject]
    public class FishAudioTTSRequest
    {
        [Key("text")]
        public string Text { get; set; }

        [Key("reference_id")]
        public string? ReferenceId { get; set; }

        [Key("format")]
        public string Format { get; set; }

        [Key("sample_rate")]
        public int SampleRate { get; set; }

        [Key("temperature")]
        public float? Temperature { get; set; }

        [Key("top_p")]
        public float? TopP { get; set; }

        [Key("prosody")]
        public FishAudioProsody? Prosody { get; set; }

        [Key("latency")]
        public string? Latency { get; set; }

        [Key("normalize")]
        public bool? Normalize { get; set; }

        [Key("repetition_penalty")]
        public float? RepetitionPenalty { get; set; }

        [Key("chunk_length")]
        public int? ChunkLength { get; set; }

        [Key("max_new_tokens")]
        public int? MaxNewTokens { get; set; }

        [Key("condition_on_previous_chunks")]
        public bool ConditionOnPreviousChunks { get; set; } = true;
    }
}

using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.FishAudio
{
    public class FishAudioConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string Model { get; set; }
        public string ReferenceId { get; set; }
        public float? Temperature { get; set; }
        public float? TopP { get; set; }
        public float? Speed { get; set; }
        public float? Volume { get; set; }
        public string? Latency { get; set; }
        public bool? Normalize { get; set; }
        public float? RepetitionPenalty { get; set; }
        public int? ChunkLength { get; set; }
        public int? MaxNewTokens { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}

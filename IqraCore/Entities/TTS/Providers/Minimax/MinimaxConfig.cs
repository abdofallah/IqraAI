using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Minimax
{
    public class MinimaxConfig : ITTSConfig
    {
        public int ConfigVersion => 1;

        public string ModelId { get; set; }
        public string VoiceId { get; set; }
        public string LanguageBoost { get; set; }

        // Voice Settings
        public float? VoiceSpeed { get; set; }
        public float? VoiceVolume { get; set; }
        public int? VoicePitch { get; set; }
        public string? VoiceEmotions { get; set; }
        public bool VoiceTextNormalization { get; set; }

        public List<string>? PronunciationDictTones { get; set; }

        // Voice Modify
        public int? VoiceModifyPitch { get; set; }
        public int? VoiceModifyIntensity { get; set; }
        public int? VoiceModifyTimbre { get; set; }
        public string? VoiceModifySoundEffects { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}

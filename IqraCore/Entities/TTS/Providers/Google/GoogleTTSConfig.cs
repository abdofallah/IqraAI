using IqraCore.Entities.Helper.Audio;
using IqraCore.Interfaces.TTS;

namespace IqraCore.Entities.TTS.Providers.Google
{
    public class GoogleTTSConfig : ITTSConfig
    {
        public int ConfigVersion => 1;  

        public string ModelType { get; set; }

        // GEMINI
        public string? GeminiModelId { get; set; }
        public string? Prompt { get; set; }

        // CHIRP
        public string? VoiceName { get; set; }
        public bool UseCustomVoiceKey { get; set; }
        public string? VoiceCloningKey { get; set; }

        // COMMON
        public string LanguageCode { get; set; }
        public string? CustomPronunciationsJson { get; set; }
        public double? SpeakingRate { get; set; }
        public double? Pitch { get; set; }

        public int TargetSampleRate { get; set; }
        public int TargetBitsPerSample { get; set; }
        public AudioEncodingTypeEnum TargetEncodingType { get; set; }
    }
}

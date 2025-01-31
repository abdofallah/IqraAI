using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.TTS
{
    public class TTSProviderSpeakerData : ProviderModelBase
    {
        public string Name { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; } = 0;
        public string PriceUnit { get; set; } = string.Empty;

        public string Gender { get; set; } = string.Empty;
        public string AgeGroup { get; set; } = string.Empty;
        public List<string> Personality { get; set; } = new List<string>();

        public List<string> SupportedLanguages { get; set; } = new List<string>();
        public bool IsMultilingual { get; set; } = false;

        public List<TTSProviderSpeakingStyleData> SpeakingStyles { get; set; } = new List<TTSProviderSpeakingStyleData>();
    }

    public class TTSProviderSpeakingStyleData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty;
        public bool IsDefault { get; set; } = false;
    }
}

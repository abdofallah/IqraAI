using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.TTS
{
    public class TTSProviderModelData : ProviderModelBase
    {
        public string Name { get; set; } = string.Empty;

        public decimal PricePerUnit { get; set; } = 0;
        public string PriceUnit { get; set; } = string.Empty;

        public List<string> SupportedLanguages { get; set; } = new List<string>();
    }
}

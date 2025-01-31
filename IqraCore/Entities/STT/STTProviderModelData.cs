using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.STT
{
    public class STTProviderModelData : ProviderModelBase
    {
        public string Name { get; set; } = "";

        public decimal PricePerUnit { get; set; } = 0;
        public string PriceUnit { get; set; } = "";
        public List<string> SupportedLanguages { get; set; } = new List<string>();
    }
}

namespace IqraCore.Entities.STT
{
    public class STTProviderModelData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime? DisabledAt { get; set; } = null;

        public decimal PricePerUnit { get; set; } = 0;
        public string PriceUnit { get; set; } = "";
        public List<string> SupportedLanguages { get; set; } = new List<string>();
    }
}

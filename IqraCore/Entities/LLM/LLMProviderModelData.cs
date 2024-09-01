namespace IqraCore.Entities.LLM
{
    public class LLMProviderModelData
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public DateTime? DisabledAt { get; set; }

        public decimal InputPrice { get; set; }
        public int InputPriceTokenUnit { get; set; }

        public decimal OutputPrice { get; set; }
        public int OutputPriceTokenUnit { get; set; }

        public int MaxInputTokenLength { get; set; }
        public int MaxOutputTokenLength { get; set; }
    }
}

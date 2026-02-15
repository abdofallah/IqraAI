using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.LLM
{
    public class LLMProviderModelData : ProviderModelBase
    {
        public string Name { get; set; } = "";

        public decimal InputPrice { get; set; } = 0;
        public int InputPriceTokenUnit { get; set; } = 0;

        public decimal OutputPrice { get; set; } = 0;
        public int OutputPriceTokenUnit { get; set; } = 0;

        public int MaxInputTokenLength { get; set; } = 0;
        public int MaxOutputTokenLength { get; set; } = 0;
    }
}

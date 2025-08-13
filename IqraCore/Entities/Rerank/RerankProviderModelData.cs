using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.Rerank
{
    public class RerankProviderModelData : ProviderModelBase
    {
        public string Name { get; set; } = "";

        public decimal Price { get; set; } = 0;
        public int PriceTokenUnit { get; set; } = 0;
    }
}

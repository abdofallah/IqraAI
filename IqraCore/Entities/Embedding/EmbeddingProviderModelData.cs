using IqraCore.Entities.ProviderBase;

namespace IqraCore.Entities.Embedding
{
    public class EmbeddingProviderModelData : ProviderModelBase
    {
        public string Name { get; set; } = "";

        public decimal Price { get; set; } = 0;
        public int PriceTokenUnit { get; set; } = 0;

        public List<int> AvailableVectorDimensions { get; set; } = new List<int>();
    }
}

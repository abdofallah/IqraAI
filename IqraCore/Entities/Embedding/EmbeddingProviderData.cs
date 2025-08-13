using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Embedding
{
    public class EmbeddingProviderData : ProviderDataBase<EmbeddingProviderModelData>
    {
        [BsonId]
        public InterfaceEmbeddingProviderEnum Id { get; set; } = InterfaceEmbeddingProviderEnum.Unknown;
    }
}

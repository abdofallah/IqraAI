using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Rerank
{
    public class RerankProviderData : ProviderDataBase<RerankProviderModelData>
    {
        [BsonId]
        public InterfaceRerankProviderEnum Id { get; set; } = InterfaceRerankProviderEnum.Unknown;
    }
}

using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.STT
{
    public class STTProviderData : ProviderDataBase<STTProviderModelData>
    {
        [BsonId]
        public InterfaceSTTProviderEnum Id { get; set; } = InterfaceSTTProviderEnum.Unknown;
    }
}

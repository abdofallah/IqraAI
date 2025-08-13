using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.LLM
{
    public class LLMProviderData : ProviderDataBase<LLMProviderModelData>
    {
        [BsonId]
        public InterfaceLLMProviderEnum Id { get; set; } = InterfaceLLMProviderEnum.Unknown;
    }
}

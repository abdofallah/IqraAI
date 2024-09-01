using IqraCore.Entities.Interfaces;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.LLM
{
    public class LLMProviderData
    {
        [BsonId]
        public InterfaceLLMProviderEnum Id { get; set; } = InterfaceLLMProviderEnum.Unknown;

        public DateTime? DisabledAt { get; set; } = null;

        public List<LLMProviderModelData> LLMProviderModelData { get; set; } = new List<LLMProviderModelData>();
    }
}

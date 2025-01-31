using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.TTS
{
    public class TTSProviderData : ProviderDataBase<TTSProviderSpeakerData>
    {
        [BsonId]
        public InterfaceTTSProviderEnum Id { get; set; } = InterfaceTTSProviderEnum.Unknown;
    }
}

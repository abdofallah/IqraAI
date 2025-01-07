using IqraCore.Entities.Interfaces;
using IqraCore.Entities.STT;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.TTS
{
    public class TTSProviderData
    {
        [BsonId]
        public InterfaceTTSProviderEnum Id { get; set; } = InterfaceTTSProviderEnum.Unknown;
        public DateTime? DisabledAt { get; set; } = null;
        public List<TTSProviderSpeakerData> Speakers { get; set; } = new List<TTSProviderSpeakerData>();
        public string IntegrationId { get; set; } = "";
        public List<TTSProviderUserIntegrationFieldData> UserIntegrationFields { get; set; } = new List<TTSProviderUserIntegrationFieldData>();
    }
}

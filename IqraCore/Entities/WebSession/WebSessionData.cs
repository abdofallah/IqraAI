using IqraCore.Entities.WebSession.Enum;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.WebSession
{
    public class WebSessionData
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public WebSessionStatusEnum Status { get; set; } = WebSessionStatusEnum.Queued;

        public long BusinessId { get; set; }
        public string WebCampaignId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;
        public string ClientIdentifier { get; set; } = string.Empty;
        public WebSessionAudioInputConfigurationData AudioInputConfiguration { get; set; } = new WebSessionAudioInputConfigurationData();
        public WebSessionAudioOutputConfigurationData AudioOutputConfiguration { get; set; } = new WebSessionAudioOutputConfigurationData();
        public Dictionary<string, string> DynamicVariables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public WebSessionTransportTypeEnum TransportType { get; set; } = WebSessionTransportTypeEnum.WebSocket;

        // If Session Created
        public string? SessionRegionBackendServerId { get; set; } = null;
        public string? SessionWebSocketUrl { get; set; } = null;
        public string? SessionId { get; set; } = null;  
    }
}

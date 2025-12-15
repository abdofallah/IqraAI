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

        public List<WebSessionLog> Logs { get; set; } = new List<WebSessionLog>();

        public WebSessionTransportTypeEnum TransportType { get; set; } = WebSessionTransportTypeEnum.WebSocket;

        // If Session Created
        public string? SessionRegionBackendServerId { get; set; } = null;
        public string? SessionWebSocketUrl { get; set; } = null;
        public string? SessionId { get; set; } = null;  
    }

    public class WebSessionLog
    {
        public WebSessionLogTypeEnum Type { get; set; } = WebSessionLogTypeEnum.Information;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }

    public enum WebSessionLogTypeEnum
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }
}

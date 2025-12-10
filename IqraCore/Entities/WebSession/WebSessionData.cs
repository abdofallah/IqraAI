using IqraCore.Entities.Helper.Audio;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.WebSession
{
    public class WebSessionData
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

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

        // If Session Created
        public string? SessionRegionBackendServerId { get; set; } = null;
        public string? SessionWebSocketUrl { get; set; } = null;
        public string? SessionId { get; set; } = null;  
    }

    public enum WebSessionStatusEnum
    {
        Queued = 0,
        ProcessingQueue = 1,
        ProcessingBackend = 2,
        ProcessedBackend = 3,
        Failed = 4,
        Canceled = 5,
        Expired = 6
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

    public class WebSessionAudioInputConfigurationData
    {
        public int SampleRate { get; set; } = 0;
        public int BitsPerSample { get; set; } = 0;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; } = AudioEncodingTypeEnum.PCM;
    }

    public class WebSessionAudioOutputConfigurationData
    {
        public int SampleRate { get; set; } = 0;
        public int BitsPerSample { get; set; } = 0;
        public AudioEncodingTypeEnum AudioEncodingType { get; set; } = AudioEncodingTypeEnum.PCM;
        public int FrameDurationMs { get; set; } = 0;
    }
}

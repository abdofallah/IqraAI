using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.WebSession
{
    public class WebSessionLogsData
    {
        [BsonId]
        public string Id { get; set; }

        public List<WebSessionLogEntry> Logs { get; set; } = new List<WebSessionLogEntry>();
    }

    public class WebSessionLogEntry
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

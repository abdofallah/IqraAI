using IqraCore.Entities.Helper.Call.Queue;

namespace IqraCore.Entities.Call.Queue
{
    public class CallQueueLogEntry
    {
        public CallQueueLogTypeEnum Type { get; set; } = CallQueueLogTypeEnum.Information;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
    }
}

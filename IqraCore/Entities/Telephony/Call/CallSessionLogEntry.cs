using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Telephony.Call
{
    public class CallSessionLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public CallSessionLogLevelEnum Level { get; set; } = CallSessionLogLevelEnum.Info;
        public string? Component { get; set; } = null;
    }
}

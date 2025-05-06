using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Entities.Call.Queue
{
    public class InboundCallQueueData : CallQueueData
    {
        public string RouteId { get; set; } = string.Empty;

        public string RouteNumberId { get; set; } = string.Empty;
        public TelephonyProviderEnum RouteNumberProvider { get; set; } = TelephonyProviderEnum.Unknown;
        public string ProviderCallId { get; set; } = string.Empty;
        public string CallerNumber { get; set; } = string.Empty;
    }
}

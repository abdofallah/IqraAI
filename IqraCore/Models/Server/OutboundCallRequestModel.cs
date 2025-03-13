namespace IqraCore.Models.Server
{
    public class OutboundCallRequestModel
    {
        public string QueueId { get; set; } = string.Empty;
        public long BusinessId { get; set; }
        public string PhoneNumberId { get; set; } = string.Empty;
        public string ToNumber { get; set; } = string.Empty;
        public string RouteId { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

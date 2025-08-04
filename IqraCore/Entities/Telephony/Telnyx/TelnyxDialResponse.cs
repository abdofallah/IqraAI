namespace IqraCore.Entities.Telephony.Telnyx
{
    public class TelnyxDialResponse
    {
        public TelnyxDialResponseData Data { get; set; }
    }

    public class TelnyxDialResponseData
    {
        public string RecordType { get; set; }
        public string CallSessionId { get; set; }
        public string CallLegId { get; set; }
        public string CallControlId { get; set; }
        public bool IsAlive { get; set; }
    }
}

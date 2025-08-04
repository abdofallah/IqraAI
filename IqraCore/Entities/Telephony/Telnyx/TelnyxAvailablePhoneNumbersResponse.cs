namespace IqraCore.Entities.Telephony.Telnyx
{
    public class TelnyxAvailablePhoneNumbersResponse
    {
        public List<TelnyxAvailablePhoneNumber> Data { get; set; }
    }

    public class TelnyxAvailablePhoneNumber
    {
        public string RecordType { get; set; }
        public string PhoneNumber { get; set; }
    }
}

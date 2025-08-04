namespace IqraCore.Entities.Telephony.Vonage
{
    public class VonageNumbersResponse
    {
        public int Count { get; set; }
        public List<VonageNumber> Numbers { get; set; }
    }

    public class VonageNumber
    {
        public string Country { get; set; }
        public string Msisdn { get; set; }
        public string Type { get; set; }
        public string Cost { get; set; }
        public List<string> Features { get; set; }
    }
}

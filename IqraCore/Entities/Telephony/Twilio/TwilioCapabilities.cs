using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.Twilio
{
    public class TwilioCapabilities
    {
        [JsonPropertyName("voice")]
        public bool Voice { get; set; }

        [JsonPropertyName("sms")]
        public bool SMS { get; set; }

        [JsonPropertyName("mms")]
        public bool MMS { get; set; }

        [JsonPropertyName("fax")]
        public bool Fax { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.Twilio
{
    public class TwilioPhoneNumberListResponse
    {
        [JsonPropertyName("incoming_phone_numbers")]
        public List<TwilioPhoneNumberDetails> IncomingPhoneNumbers { get; set; } = new List<TwilioPhoneNumberDetails>();

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("first_page_uri")]
        public string FirstPageUri { get; set; } = string.Empty;

        [JsonPropertyName("next_page_uri")]
        public string? NextPageUri { get; set; }

        [JsonPropertyName("previous_page_uri")]
        public string? PreviousPageUri { get; set; }
    }
}

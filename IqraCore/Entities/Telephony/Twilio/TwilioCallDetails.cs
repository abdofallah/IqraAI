using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.Twilio
{
    public class TwilioCallDetails
    {
        [JsonPropertyName("sid")]
        public string Sid { get; set; } = string.Empty;

        [JsonPropertyName("account_sid")]
        public string AccountSid { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("phone_number_sid")]
        public string PhoneNumberSid { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("start_time")]
        public string? StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }

        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = string.Empty;

        [JsonPropertyName("answered_by")]
        public string? AnsweredBy { get; set; }

        [JsonPropertyName("api_version")]
        public string ApiVersion { get; set; } = string.Empty;

        [JsonPropertyName("annotation")]
        public string? Annotation { get; set; }

        [JsonPropertyName("forwarded_from")]
        public string? ForwardedFrom { get; set; }

        [JsonPropertyName("group_sid")]
        public string? GroupSid { get; set; }

        [JsonPropertyName("caller_name")]
        public string? CallerName { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("subresource_uris")]
        public Dictionary<string, string>? SubresourceUris { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.Twilio
{
    public class TwilioPhoneNumberDetails
    {
        [JsonPropertyName("sid")]
        public string Sid { get; set; } = string.Empty;

        [JsonPropertyName("account_sid")]
        public string AccountSid { get; set; } = string.Empty;

        [JsonPropertyName("friendly_name")]
        public string FriendlyName { get; set; } = string.Empty;

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("capabilities")]
        public TwilioCapabilities Capabilities { get; set; } = new TwilioCapabilities();

        [JsonPropertyName("voice_url")]
        public string VoiceUrl { get; set; } = string.Empty;

        [JsonPropertyName("voice_method")]
        public string VoiceMethod { get; set; } = string.Empty;

        [JsonPropertyName("sms_url")]
        public string SmsUrl { get; set; } = string.Empty;

        [JsonPropertyName("sms_method")]
        public string SmsMethod { get; set; } = string.Empty;

        [JsonPropertyName("status_callback")]
        public string StatusCallback { get; set; } = string.Empty;

        [JsonPropertyName("status_callback_method")]
        public string StatusCallbackMethod { get; set; } = string.Empty;

        [JsonPropertyName("date_created")]
        public string DateCreated { get; set; } = string.Empty;

        [JsonPropertyName("date_updated")]
        public string DateUpdated { get; set; } = string.Empty;
    }
}

using System.Text.Json.Serialization;

namespace IqraCore.Entities.Telephony.ModemTel
{
    public class ModemTelWebhookData
    {
        [JsonPropertyName("event_type")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; } = null;

        [JsonPropertyName("data")]
        public virtual dynamic? Data { get; set; } = null;
    }

    // INCOMING CALL
    public class ModemTelWebhookIncomingCallData
    {
        [JsonPropertyName("call_id")]
        public string CallId { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = string.Empty;

        [JsonPropertyName("media")]
        public ModemTelWebhookIncomingCallMediaData Media { get; set; } = new();
    }
    public class ModemTelWebhookIncomingCallMediaData
    {
        [JsonPropertyName("websocket_url")]
        public string WebSocketURL { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
    }

    // ANSWERED CALL
    public class ModemTelWebhookAnsweredCallData
    {
        [JsonPropertyName("call_id")]
        public string CallId { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }

    // ENDED CALL
    public class ModemTelWebhookEndedCallData
    {
        [JsonPropertyName("call_id")]
        public string CallId { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }

    // MISSED CALL
    public class ModemTelWebhookMissedCallData
    {
        [JsonPropertyName("call_id")]
        public string CallId { get; set; } = string.Empty;

        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public string To { get; set; } = string.Empty;
    }
}

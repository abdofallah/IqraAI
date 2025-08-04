using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class VonageConversationClient : BaseTelephonyConversationClient
    {
        private readonly VonageManager _vonageManager;
        private readonly string _callUuid; // Vonage's equivalent of Call SID

        public VonageConversationClient(
            string clientId,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string callUuid,
            VonageManager vonageManager,
            IConversationClientTransport transport,
            ILogger<VonageConversationClient> logger
        ) : base(clientId, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _callUuid = callUuid;
            _vonageManager = vonageManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.Vonage;
        }

        /// <summary>
        /// Vonage does not send distinct events like 'start' or 'stop' over the WebSocket.
        /// It primarily sends audio payloads and DTMF tones as JSON objects.
        /// We will parse incoming text messages for these payloads.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            JsonNode? jsonMessage;
            try
            {
                jsonMessage = JsonNode.Parse(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse incoming JSON from Vonage: {Message}", message);
                return;
            }

            // Check if the message is a DTMF event
            if (jsonMessage["type"]?.GetValue<string>() == "dtmf")
            {
                string? digit = jsonMessage["dtmf"]?["digit"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(digit))
                {
                    RaiseDTMFReceived(digit);
                }
            }
            // Otherwise, assume it's a media message (audio)
            else if (jsonMessage["media"]?["payload"] is not null)
            {
                string? payload = jsonMessage["media"]?["payload"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(payload))
                {
                    try
                    {
                        byte[] audioData = Convert.FromBase64String(payload);
                        RaiseAudioReceived(audioData);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning(ex, "Received malformed Base64 audio payload from Vonage.");
                    }
                }
            }
        }

        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            _logger.LogWarning("Received an unexpected binary message on a Vonage client stream. Ignoring {DataLength} bytes.", data.Length);
        }

        /// <summary>
        /// Sends audio to Vonage by wrapping it in their expected JSON format.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            var mediaPayloadBase64 = Convert.ToBase64String(audioData);
            // Vonage's format for sending audio is simpler, just the payload.
            var mediaMessage = $"{{\"media\":{{\"payload\":\"{mediaPayloadBase64}\"}} }}";
            return Transport.SendTextAsync(mediaMessage, cancellationToken);
        }

        /// <summary>
        /// Sends DTMF digits by wrapping them in Vonage's expected JSON format.
        /// </summary>
        public override Task SendDTMFAsync(string digits, CancellationToken cancellationToken)
        {
            // This would require using the Vonage REST API to send DTMF, not the WebSocket.
            // This is a key difference from Twilio.
            _logger.LogInformation("Sending DTMF for call {CallUuid} via Vonage REST API.", _callUuid);
            return _vonageManager.SendDtmfAsync(_callUuid, digits);
        }

        /// <summary>
        /// Disconnects the call by closing the transport and then using the Vonage API to hang up.
        /// </summary>
        public override async Task DisconnectAsync(string reason)
        {
            await base.DisconnectAsync(reason); // Closes WebSocket
            _logger.LogInformation("Ending Vonage call {CallUuid} via API.", _callUuid);
            await _vonageManager.EndCallAsync(_callUuid);
        }
    }
}

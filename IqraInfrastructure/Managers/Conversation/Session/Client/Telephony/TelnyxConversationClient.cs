using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class TelnyxConversationClient : BaseTelephonyConversationClient
    {
        private readonly TelnyxManager _telnyxManager;
        private readonly string _callControlId; // Telnyx's unique call identifier
        private readonly string? _streamId; // Telnyx uses a stream_url for connection

        public TelnyxConversationClient(
            string clientId,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string callControlId,
            string? streamId,
            TelnyxManager telnyxManager,
            IConversationClientTransport transport,
            ILogger<TelnyxConversationClient> logger
        ) : base(clientId, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _callControlId = callControlId;
            _streamId = streamId;
            _telnyxManager = telnyxManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.Telnyx;
        }

        /// <summary>
        /// Handles incoming text messages from Telnyx, which are JSON payloads.
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
                _logger.LogError(ex, "Failed to parse incoming JSON from Telnyx: {Message}", message);
                return;
            }

            string? eventType = jsonMessage["event"]?.GetValue<string>();

            switch (eventType)
            {
                case "media":
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
                            _logger.LogWarning(ex, "Received malformed Base64 audio payload from Telnyx.");
                        }
                    }
                    break;
                case "dtmf":
                    string? digit = jsonMessage["dtmf"]?["digit"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(digit))
                    {
                        RaiseDTMFReceived(digit);
                    }
                    break;
            }
        }

        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            _logger.LogWarning("Received an unexpected binary message on a Telnyx client stream. Ignoring {DataLength} bytes.", data.Length);
        }

        /// <summary>
        /// Sends audio to Telnyx by wrapping it in their specific JSON format.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            var mediaPayloadBase64 = Convert.ToBase64String(audioData);
            var mediaMessage = new { command = "media", payload = mediaPayloadBase64, stream_id = _streamId };
            string jsonPayload = JsonSerializer.Serialize(mediaMessage);

            return Transport.SendTextAsync(jsonPayload, cancellationToken);
        }

        /// <summary>
        /// Sends DTMF digits using the Telnyx API.
        /// </summary>
        public override Task SendDTMFAsync(string digits, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Sending DTMF for call {CallControlId} via Telnyx API.", _callControlId);
            return _telnyxManager.SendDtmfAsync(_callControlId, digits);
        }

        /// <summary>
        /// Disconnects the call by closing the transport and then using the Telnyx API.
        /// </summary>
        public override async Task DisconnectAsync(string reason)
        {
            await base.DisconnectAsync(reason); // Closes WebSocket
            _logger.LogInformation("Ending Telnyx call {CallControlId} via API.", _callControlId);
            await _telnyxManager.EndCallAsync(_callControlId);
        }
    }
}

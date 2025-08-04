using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public class TwilioConversationClient : BaseTelephonyConversationClient
    {
        private readonly TwilioManager _twilioManager;
        private readonly string? _providerCallSid;
        private readonly string _accountSid;
        private readonly string _authToken;
        private string? _streamSidFromTwilio;

        public TwilioConversationClient(
            string clientId,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            string? providerCallSid,
            string accountSid,
            string authToken,
            TwilioManager twilioManager,
            IConversationClientTransport transport,
            ILogger<TwilioConversationClient> logger
        ) : base(clientId, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, transport, logger)
        {
            _providerCallSid = providerCallSid;
            _accountSid = accountSid;
            _authToken = authToken;
            _twilioManager = twilioManager;
            ClientTelephonyProviderType = TelephonyProviderEnum.Twilio;
        }

        /// <summary>
        /// Handles incoming text messages from the transport. For Twilio, these are JSON payloads.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            JsonNode? genericMessage;
            try
            {
                genericMessage = JsonNode.Parse(message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse incoming JSON from Twilio: {Message}", message);
                return;
            }

            if (genericMessage == null) return;

            string? eventType = genericMessage["event"]?.GetValue<string>();

            switch (eventType)
            {
                case "start":
                    _streamSidFromTwilio = genericMessage["streamSid"]?.GetValue<string>();
                    _logger.LogInformation("Twilio stream started with SID: {StreamSid}", _streamSidFromTwilio);
                    break;

                case "media":
                    if ("inbound".Equals(genericMessage["media"]?["track"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase))
                    {
                        string? payload = genericMessage["media"]?["payload"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(payload))
                        {
                            try
                            {
                                byte[] audioData = Convert.FromBase64String(payload);
                                RaiseAudioReceived(audioData); // Use the protected invoker
                            }
                            catch (FormatException ex)
                            {
                                _logger.LogWarning(ex, "Received malformed Base64 audio payload from Twilio.");
                            }
                        }
                    }
                    break;

                case "dtmf":
                    if ("inbound".Equals(genericMessage["dtmf"]?["track"]?.GetValue<string>(), StringComparison.OrdinalIgnoreCase))
                    {
                        string? digit = genericMessage["dtmf"]?["digit"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(digit))
                        {
                            RaiseDTMFReceived(digit); // Use the protected invoker
                        }
                    }
                    break;

                case "stop":
                    _logger.LogInformation("Received 'stop' event from Twilio for stream {StreamSid}.", _streamSidFromTwilio);
                    // The transport will fire its own Disconnected event, which we handle in OnTransportDisconnected.
                    break;
            }
        }

        /// <summary>
        /// Twilio sends audio via text messages (JSON), so receiving a raw binary frame is unexpected.
        /// </summary>
        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            _logger.LogWarning("Received an unexpected binary message on a Twilio client stream. Ignoring {DataLength} bytes.", data.Length);
        }

        /// <summary>
        /// Sends audio data by wrapping it in Twilio's expected JSON format and sending it as a text message.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_streamSidFromTwilio))
            {
                _logger.LogWarning("Cannot send audio, Twilio stream SID is not yet established.");
                return Task.CompletedTask;
            }

            var mediaPayloadBase64 = Convert.ToBase64String(audioData);
            var mediaMessage = $"{{\"event\":\"media\",\"streamSid\":\"{_streamSidFromTwilio}\",\"media\":{{\"payload\":\"{mediaPayloadBase64}\"}} }}";

            // Delegate sending to the transport
            return Transport.SendTextAsync(mediaMessage, cancellationToken);
        }

        public override Task SendDTMFAsync(string digits, CancellationToken cancellationToken)
        {
            // Implementation remains similar, but uses the transport
            var dtmfPayload = new { @event = "dtmf", streamSid = _streamSidFromTwilio, dtmf = new { digits } };
            string jsonPayload = JsonSerializer.Serialize(dtmfPayload);
            return Transport.SendTextAsync(jsonPayload, cancellationToken);
        }

        public Task ClearBufferedAudioAync(CancellationToken cancellationToken)
        {
            // Implementation remains similar, but uses the transport
            var clearPayload = new { @event = "clear", streamSid = _streamSidFromTwilio };
            string jsonPayload = JsonSerializer.Serialize(clearPayload);
            return Transport.SendTextAsync(jsonPayload, cancellationToken);
        }

        /// <summary>
        /// Overrides the base disconnect to add Twilio-specific logic (ending the call via API).
        /// </summary>
        public override async Task DisconnectAsync(string reason)
        {
            // First, tell the transport to disconnect the underlying connection (e.g., WebSocket)
            await base.DisconnectAsync(reason);

            // Then, perform the provider-specific cleanup action
            if (!string.IsNullOrEmpty(_providerCallSid))
            {
                _logger.LogInformation("Ending Twilio call leg {CallSid} via API.", _providerCallSid);
                await _twilioManager.EndCallAsync(_accountSid, _authToken, _providerCallSid);
            }
        }
    }
}
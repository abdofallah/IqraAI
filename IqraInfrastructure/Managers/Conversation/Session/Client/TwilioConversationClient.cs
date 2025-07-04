using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public class TwilioConversationClient : WebSocketCapableConversationClient
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
            ILogger<TwilioConversationClient> logger
        ) : base(clientId, telephonyPhoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, logger)
        {
            _providerCallSid = providerCallSid;
            _accountSid = accountSid;
            _authToken = authToken;
            _twilioManager = twilioManager;
            _clientTelephonyProviderType = TelephonyProviderEnum.Twilio;
        }

        public override async Task HandleAcceptedWebSocketAsync(WebSocket webSocket, CancellationToken sessionCts)
        {
            await base.HandleAcceptedWebSocketAsync(webSocket, sessionCts);
        }

        protected override Task ProcessReceivedBinaryFrameAsync(byte[] data, CancellationToken cancellationToken)
        {
            // Twilio sends audio as Base64 in JSON text frames via <Stream>
            // This method might not be directly called if only text frames are expected.
            return Task.CompletedTask;
        }

        protected override Task ProcessReceivedTextFrameAsync(string message, CancellationToken cancellationToken)
        {
            JsonNode? genericMessage = null;
            try
            {
                genericMessage = JsonNode.Parse(message);
            }
            catch (JsonException)
            {
                return Task.CompletedTask; // Invalid JSON
            }
            if (genericMessage == null) return Task.CompletedTask;

            string? eventType = genericMessage["event"]?.GetValue<string>();

            switch (eventType)
            {
                case "start":
                    _streamSidFromTwilio = genericMessage["streamSid"]?.GetValue<string>();
                    break;
                case "media":
                    string? track = genericMessage["media"]?["track"]?.GetValue<string>();
                    if ("inbound".Equals(track, StringComparison.OrdinalIgnoreCase))
                    {
                        string? payload = genericMessage["media"]?["payload"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(payload))
                        {
                            try
                            {
                                byte[] audioData = Convert.FromBase64String(payload);
                                OnAudioReceived(audioData);
                            }
                            catch (FormatException) { /* Log bad base64 */ }
                        }
                    }
                    break;
                case "dtmf":
                    string? dtmfTrack = genericMessage["dtmf"]?["track"]?.GetValue<string>();
                    if ("inbound".Equals(dtmfTrack, StringComparison.OrdinalIgnoreCase))
                    {
                        string? digit = genericMessage["dtmf"]?["digit"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(digit))
                        {
                            OnDTMFRecieved(digit);
                        }
                    }
                    break;
                case "stop":
                    _ = HandleWebSocketErrorAndDisconnect("Media stream stopped by Twilio event."); // Fire and forget
                    break;
            }
            return Task.CompletedTask;
        }

        public override async Task SendDTMFAsync(string digits, CancellationToken cancellationToken)
        {
            if (!_isConnected || _activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open || string.IsNullOrEmpty(_streamSidFromTwilio))
            {
                return;
            }

            var dtmfPayload = new
            {
                @event = "dtmf",
                streamSid = _streamSidFromTwilio,
                dtmf = new { digits }
            };
            string jsonPayload = JsonSerializer.Serialize(dtmfPayload);
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(jsonPayload);
                await SendWebSocketDataAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, cancellationToken);
            }
            catch (Exception ex)
            {
                await base.HandleWebSocketErrorAndDisconnect($"Error sending Twilio DTMF: {ex.Message}");
                throw;
            }
        }

        public override async Task ClearBufferedAudioAync(CancellationToken cancellationToken)
        {
            if (!_isConnected || _activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open || string.IsNullOrEmpty(_streamSidFromTwilio))
            {
                return;
            }

            var clearAudioPayload = new
            {
                @event = "clear",
                streamSid = _streamSidFromTwilio
            };
            string jsonPayload = JsonSerializer.Serialize(clearAudioPayload);
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(jsonPayload);
                await SendWebSocketDataAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, cancellationToken);
            }
            catch (Exception ex)
            {
                await base.HandleWebSocketErrorAndDisconnect($"Error sending Twilio clear audio buffer: {ex.Message}");
                throw;
            }
        }

        public override async Task DisconnectAsync(string reason)
        {
            bool wasConnected = _isConnected;
            await base.DisconnectAsync(reason);

            if (wasConnected && !string.IsNullOrEmpty(_providerCallSid))
            {
                await _twilioManager.EndCallAsync(_accountSid, _authToken, _providerCallSid);
            }
        }
    }
}
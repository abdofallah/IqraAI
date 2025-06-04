using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Client
{
    public class ModemTelConversationClient : WebSocketCapableConversationClient
    {
        private readonly ModemTelManager _modemTelManager;
        private readonly string? _providerCallId;
        private readonly string _apiKey;
        private readonly string _apiBaseUrl;
        private bool _callAnsweredByProviderApi = false;


        public ModemTelConversationClient(
            string clientId,
            string clientPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string? providerCallId,
            string apiBaseUrl,
            string apiKey,
            ModemTelManager modemTelManager,
            ILogger<ModemTelConversationClient> logger
        ) : base(clientId, clientPhoneNumber, telephonyProviderPhoneNumberId, logger)
        {
            _providerCallId = providerCallId;
            _apiBaseUrl = apiBaseUrl;
            _apiKey = apiKey;
            _modemTelManager = modemTelManager;
            _clientTelephonyProviderType = TelephonyProviderEnum.ModemTel;
        }

        public override async Task HandleAcceptedWebSocketAsync(WebSocket webSocket, CancellationToken sessionCts)
        {
            await base.HandleAcceptedWebSocketAsync(webSocket, sessionCts);
            // Any ModemTel specific logic after WebSocket is accepted by base can go here.
        }

        protected override Task ProcessReceivedBinaryFrameAsync(byte[] data, CancellationToken cancellationToken)
        {
            OnAudioReceived(data);
            return Task.CompletedTask;
        }

        protected override Task ProcessReceivedTextFrameAsync(string message, CancellationToken cancellationToken)
        {
            if (message.StartsWith("DTMF:"))
            {
                var digit = message.Substring("DTMF:".Length);
                if (!string.IsNullOrEmpty(digit))
                {
                    OnDTMFRecieved(digit);
                }
            }
            return Task.CompletedTask;
        }

        public override async Task SendDTMFAsync(string digits, CancellationToken cancellationToken)
        {
            if (!_isConnected || _activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open)
            {
                return;
            }
            string dtmfMessage = $"DTMF:{digits}";
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(dtmfMessage);
                await base.SendWebSocketDataAsync(new System.ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, cancellationToken);
            }
            catch (System.Exception ex)
            {
                await base.HandleWebSocketErrorAndDisconnect($"Error sending DTMF: {ex.Message}");
                throw;
            }
        }

        public override async Task ClearBufferedAudioAync(CancellationToken cancellationToken)
        {
            if (!_isConnected || _activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open)
            {
                return;
            }
            string clearMessage = $"clear";
            try
            {
                var messageBytes = Encoding.UTF8.GetBytes(clearMessage);
                await base.SendWebSocketDataAsync(new System.ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, cancellationToken);
            }
            catch (System.Exception ex)
            {
                await base.HandleWebSocketErrorAndDisconnect($"Error sending DTMF: {ex.Message}");
                throw;
            }
        }

        public override async Task DisconnectAsync(string reason)
        {
            bool wasConnected = _isConnected; // Check before calling base.DisconnectAsync
            await base.DisconnectAsync(reason); // Handles WebSocket closure and sets _isConnected = false

            if (wasConnected && !string.IsNullOrEmpty(_providerCallId))
            {
                await _modemTelManager.HangupCallAsync(_apiKey, _apiBaseUrl, _providerCallId);
            }
            // OnDisconnected event is called by base.DisconnectAsync
        }
    }
}
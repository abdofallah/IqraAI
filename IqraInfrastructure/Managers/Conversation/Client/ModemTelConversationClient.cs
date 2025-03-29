using IqraCore.Entities.Helper.Telephony;
using IqraInfrastructure.Managers.Telephony;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Client
{
    public class ModemTelConversationClient : BaseTelephonyConversationClient
    {
        private readonly ModemTelManager _modemTelManager;
        private readonly string _callId;
        private readonly string _apiKey;
        private readonly string _apiBaseUrl;
        private readonly string _mediaSessionToken;
        private ClientWebSocket? _webSocket;
        private Task? _receiveTask;
        private readonly int _bufferSize = 4096;

        private bool _isAnswered = false;

        public string CallId => _callId;

        public ModemTelConversationClient(
            string clientId,
            string phoneNumber,
            string callId,
            string apiBaseUrl,
            string apiKey,       
            string mediaSessionToken,
            ModemTelManager modemTelManager,
            ILogger<ModemTelConversationClient> logger)
            : base(clientId, phoneNumber, logger)
        {
            _callId = callId;
            _apiKey = apiKey;
            _apiBaseUrl = apiBaseUrl;
            _mediaSessionToken = mediaSessionToken;
            _modemTelManager = modemTelManager;
            _clientTelephonyType = TelephonyProviderEnum.ModemTel;
        }

        public override async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (!_isAnswered)
            {
                var answerResult = await _modemTelManager.AnswerCallAsync(_apiKey, _apiBaseUrl, _callId);
                if (!answerResult.Success)
                {
                    _logger.LogError("Error answering call {CallId}: {ErrorMessage}", _callId, answerResult.Message);
                    return;
                }
                _isAnswered = true;
            }

            if (_isConnected)
            {
                _logger.LogWarning("WebSocket is already connected for call {CallId}", _callId);
                return;
            }

            try
            {
                _connectionCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Create a new WebSocket
                _webSocket = new ClientWebSocket();
                _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {_mediaSessionToken}");

                // Connect to the WebSocket
                var currentBaseUrl = _apiBaseUrl;
                if (currentBaseUrl.StartsWith("http://") || currentBaseUrl.StartsWith("https://"))
                {
                    currentBaseUrl = currentBaseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
                }
                var baseUri = new Uri(currentBaseUrl);
                baseUri = new Uri(baseUri, $"ws/calls/{_callId}");
                await _webSocket.ConnectAsync(baseUri, _connectionCts.Token);

                _logger.LogInformation("Connected to ModemTel WebSocket for call {CallId}", _callId);

                // Start receiving data
                _receiveTask = Task.Run(() => ReceiveLoopAsync(_connectionCts.Token), _connectionCts.Token);

                _isConnected = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to ModemTel WebSocket for call {CallId}", _callId);
                throw;
            }
        }

        public override async Task DisconnectAsync(string reason)
        {
            if (!_isConnected)
            {
                _logger.LogDebug("WebSocket is already disconnected for call {CallId}", _callId);
                return;
            }

            try
            {
                // Cancel the receive task
                _connectionCts?.Cancel();

                // Close the WebSocket if it's still open
                if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        reason,
                        CancellationToken.None);
                }

                // End the call on the ModemTel side
                await _modemTelManager.HangupCallAsync(_apiKey, _apiBaseUrl, _callId);

                _logger.LogInformation("Disconnected from ModemTel WebSocket for call {CallId}: {Reason}", _callId, reason);

                // Dispose resources
                _webSocket?.Dispose();
                _webSocket = null;
                _connectionCts?.Dispose();
                _connectionCts = null;

                _isConnected = false;
                OnDisconnected(reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from ModemTel WebSocket for call {CallId}", _callId);
            }
        }

        public override async Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            if (!_isConnected || _webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send audio because WebSocket is not connected for call {CallId}", _callId);
                return;
            }

            try
            {
                // Send audio data
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(audioData),
                    WebSocketMessageType.Binary,
                    true,
                    cancellationToken);

                _logger.LogDebug("Sent {Length} bytes of audio data for call {CallId}", audioData.Length, _callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending audio data for call {CallId}", _callId);
                throw;
            }
        }

        public override async Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (!_isConnected || _webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Cannot send text because WebSocket is not connected for call {CallId}", _callId);
                return;
            }

            try
            {
                // For ModemTel, we might need to send a control message as text
                var controlMessage = new ModemTelControlMessage
                {
                    Type = "message",
                    Text = text
                };

                var json = JsonSerializer.Serialize(controlMessage);
                var buffer = Encoding.UTF8.GetBytes(json);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);

                _logger.LogDebug("Sent text message for call {CallId}: {Text}", _callId, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending text message for call {CallId}", _callId);
                throw;
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[_bufferSize];
            var ms = new MemoryStream();

            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       _webSocket != null &&
                       _webSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result;
                    try
                    {
                        result = await _webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // Normal cancellation
                        break;
                    }
                    catch (WebSocketException ex)
                    {
                        _logger.LogWarning(ex, "WebSocket error for call {CallId}", _callId);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("WebSocket closed by server for call {CallId}: {CloseStatus} {CloseDescription}",
                            _callId, result.CloseStatus, result.CloseStatusDescription);

                        await DisconnectAsync("WebSocket closed by server: " + result.CloseStatusDescription);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // Audio data
                        ms.Write(buffer, 0, result.Count);

                        if (result.EndOfMessage)
                        {
                            byte[] audioData = ms.ToArray();
                            OnAudioReceived(audioData);
                            ms.SetLength(0);
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Control message
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessControlMessageAsync(message, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error in WebSocket receive loop for call {CallId}", _callId);
                    await DisconnectAsync("Error in receive loop: " + ex.Message);
                }
            }
            finally
            {
                ms.Dispose();
            }
        }

        private async Task ProcessControlMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                var controlMessage = JsonSerializer.Deserialize<ModemTelControlMessage>(message);
                if (controlMessage == null)
                {
                    _logger.LogWarning("Invalid control message for call {CallId}: {Message}", _callId, message);
                    return;
                }

                _logger.LogDebug("Received control message: {Type} for call {CallId}", controlMessage.Type, _callId);

                switch (controlMessage.Type)
                {
                    case "call.ended":
                        _logger.LogInformation("Call {CallId} ended by provider", _callId);
                        await DisconnectAsync("Call ended by provider");
                        break;

                    case "dtmf":
                        if (!string.IsNullOrEmpty(controlMessage.Digit))
                        {
                            _logger.LogInformation("DTMF received: {Digit} for call {CallId}", controlMessage.Digit, _callId);
                            OnTextReceived($"<dtmf>{controlMessage.Digit}</dtmf>");
                        }
                        break;

                    case "message":
                        if (!string.IsNullOrEmpty(controlMessage.Text))
                        {
                            _logger.LogInformation("Text message received for call {CallId}: {Text}", _callId, controlMessage.Text);
                            OnTextReceived(controlMessage.Text);
                        }
                        break;

                    default:
                        _logger.LogDebug("Unhandled control message type: {Type} for call {CallId}", controlMessage.Type, _callId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing control message for call {CallId}: {Message}", _callId, message);
            }
        }

        private class ModemTelControlMessage
        {
            public string Type { get; set; } = string.Empty;
            public string? CallId { get; set; }
            public string? Digit { get; set; }
            public string? Text { get; set; }
        }
    }
}
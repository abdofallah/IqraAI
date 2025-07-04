using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public abstract class WebSocketCapableConversationClient : BaseTelephonyConversationClient
    {
        protected WebSocket? _activeWebSocket;
        protected CancellationTokenSource? _webSocketLoopCts;
        protected Task? _receiveLoopTask;
        protected readonly int _receiveBufferSize = 8192;

        protected WebSocketCapableConversationClient(string clientId, string phoneNumber, string telephonyProviderPhoneNumberId, string customerPhoneNumber, ILogger logger)
            : base(clientId, phoneNumber, telephonyProviderPhoneNumberId, customerPhoneNumber, logger)
        {
        }

        public virtual async Task HandleAcceptedWebSocketAsync(WebSocket webSocket, CancellationToken sessionCts)
        {
            if (_activeWebSocket != null && _activeWebSocket.State == WebSocketState.Open)
            {
                try { await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Session already has an active WebSocket", CancellationToken.None); } catch { }
                webSocket.Dispose();
                return;
            }

            _activeWebSocket = webSocket;
            _webSocketLoopCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts); // Link to overall session lifetime
            _isConnected = true; // Mark general client as connected

            _receiveLoopTask = Task.Run(() => StartReceiveLoopAsync(_webSocketLoopCts.Token), _webSocketLoopCts.Token);
        }

        protected virtual async Task StartReceiveLoopAsync(CancellationToken linkedCts)
        {
            var buffer = new ArraySegment<byte>(new byte[_receiveBufferSize]);
            try
            {
                while (_activeWebSocket!.State == WebSocketState.Open && !linkedCts.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    using var individualReceiveCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts);
                    individualReceiveCts.CancelAfter(TimeSpan.FromSeconds(60)); // Timeout for individual receive (includes keep-alive)

                    try
                    {
                        result = await _activeWebSocket.ReceiveAsync(buffer, individualReceiveCts.Token);
                    }
                    catch (OperationCanceledException) when (individualReceiveCts.IsCancellationRequested && !linkedCts.IsCancellationRequested)
                    {
                        if (_activeWebSocket != null && _activeWebSocket.State == WebSocketState.Open) // Still open, so it was our timeout
                        {
                            // Optional: Send PING if this client is responsible for initiating keep-alives
                            // await SendWebSocketDataAsync(new ArraySegment<byte>(Array.Empty<byte>()), WebSocketMessageType.Ping, linkedCts);
                            // For now, assume provider handles pings or connection drops if silent too long
                        }
                        continue; // Continue to next receive attempt
                    }
                    catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
                    {
                        break; // Master cancellation
                    }
                    catch (WebSocketException ex)
                    {
                        await HandleWebSocketErrorAndDisconnect("WebSocketException in ReceiveLoop");
                        break;
                    }


                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await HandleWebSocketErrorAndDisconnect($"WebSocket CLOSE frame received: {result.CloseStatusDescription}");
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                    {
                        var data = new byte[result.Count];
                        Array.Copy(buffer.Array!, buffer.Offset, data, 0, result.Count);
                        await ProcessReceivedBinaryFrameAsync(data, linkedCts);
                    }
                    else if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, result.Count);
                        await ProcessReceivedTextFrameAsync(message, linkedCts);
                    }
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && linkedCts.IsCancellationRequested))
            {
                await HandleWebSocketErrorAndDisconnect($"Unhandled exception in ReceiveLoop: {ex.Message}");
            }
            finally
            {
                if (_isConnected) // If still marked connected, means loop exited unexpectedly or needs formal disconnect
                {
                    await HandleWebSocketErrorAndDisconnect("Receive loop ended.");
                }
            }
        }

        protected abstract Task ProcessReceivedTextFrameAsync(string message, CancellationToken cancellationToken);

        protected virtual Task ProcessReceivedBinaryFrameAsync(byte[] data, CancellationToken cancellationToken)
        {
            OnAudioReceived(data);
            return Task.CompletedTask;
        }

        protected async Task SendWebSocketDataAsync(ArraySegment<byte> data, WebSocketMessageType messageType, CancellationToken cancellationToken)
        {
            if (_activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected or not open.");
            }
            await _activeWebSocket.SendAsync(data, messageType, true, cancellationToken);
        }

        protected virtual async Task HandleWebSocketErrorAndDisconnect(string reason)
        {
            if (_isConnected) // Avoid redundant calls if already disconnecting
            {
                // This will call the derived class's DisconnectAsync
                await DisconnectAsync(reason);
            }
        }


        public override async Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            if (!_isConnected || _activeWebSocket == null || _activeWebSocket.State != WebSocketState.Open)
            {
                return;
            }
            try
            {
                await SendWebSocketDataAsync(new ArraySegment<byte>(audioData), WebSocketMessageType.Binary, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleWebSocketErrorAndDisconnect($"Error sending audio: {ex.Message}");
                throw;
            }
        }

        public abstract Task ClearBufferedAudioAync(CancellationToken cancellationToken);

        public override async Task DisconnectAsync(string reason) // Made virtual to allow override
        {
            if (_hasDiconnected)
            {
                return;
            }
            _hasDiconnected = true;

            if (!_isConnected && _activeWebSocket == null)
            {
                OnDisconnected(reason); // Ensure event fires if called when already "disconnected"
                return;
            }
            _isConnected = false; // Set immediately

            _webSocketLoopCts?.CancelAfter(TimeSpan.FromSeconds(5)); // Signal receive loop to stop

            if (_activeWebSocket != null)
            {
                if (_activeWebSocket.State == WebSocketState.Open || _activeWebSocket.State == WebSocketState.CloseReceived)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                        await _activeWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, cts.Token);
                    }
                    catch { /* ignored */ }
                }
                try { _activeWebSocket.Dispose(); } catch { /* ignored */ }
                _activeWebSocket = null;
            }

            if (_receiveLoopTask != null && !_receiveLoopTask.IsCompleted)
            {
                try { await Task.WhenAny(_receiveLoopTask, Task.Delay(TimeSpan.FromMilliseconds(500))); } catch { }
            }
            _receiveLoopTask = null;

            _webSocketLoopCts?.Dispose();
            _webSocketLoopCts = null;

            OnDisconnected(reason);
        }

        public override void Dispose()
        {
            _webSocketLoopCts?.Cancel();
            if (_activeWebSocket != null && _activeWebSocket.State == WebSocketState.Open)
            {
                // Fire and forget close if Dispose is called abruptly
                _activeWebSocket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Client disposing", CancellationToken.None).ConfigureAwait(false);
            }
            _activeWebSocket?.Dispose();
            _webSocketLoopCts?.Dispose();
        }
    }
}
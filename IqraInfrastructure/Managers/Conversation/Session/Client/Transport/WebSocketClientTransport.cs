using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    public class WebSocketClientTransport : IConversationClientTransport
    {
        private readonly WebSocket _webSocket;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _loopCts;
        private readonly Task _receiveLoopTask;
        private readonly int _receiveBufferSize = 8192; // 8 KB buffer

        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public WebSocketClientTransport(WebSocket webSocket, ILogger logger, CancellationToken sessionCts)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Link the loop's cancellation to the broader session's cancellation
            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(sessionCts);

            // Start the process of listening for incoming messages
            _receiveLoopTask = Task.Run(() => StartReceiveLoopAsync(_loopCts.Token), _loopCts.Token);
        }

        private async Task StartReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[_receiveBufferSize]);
            try
            {
                while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) break;

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Close:
                            _logger.LogInformation("WebSocket CLOSE frame received. Reason: {Status}", result.CloseStatusDescription);
                            Disconnected?.Invoke(this, result.CloseStatusDescription ?? "WebSocket closed by remote party.");
                            return; // Exit the loop

                        case WebSocketMessageType.Binary when result.Count > 0:
                            var binaryData = new byte[result.Count];
                            Array.Copy(buffer.Array!, buffer.Offset, binaryData, 0, result.Count);
                            BinaryMessageReceived?.Invoke(this, binaryData);
                            break;

                        case WebSocketMessageType.Text when result.Count > 0:
                            var textData = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, result.Count);
                            TextMessageReceived?.Invoke(this, textData);
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("WebSocket receive loop was canceled.");
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "A WebSocket error occurred in the receive loop.");
                Disconnected?.Invoke(this, $"WebSocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred in the receive loop.");
                Disconnected?.Invoke(this, $"Unhandled Exception: {ex.Message}");
            }
            finally
            {
                _logger.LogInformation("WebSocket receive loop ended.");
                // Ensure the disconnected event is fired if the loop terminates unexpectedly.
                Disconnected?.Invoke(this, "Receive loop terminated.");
            }
        }

        public async Task SendBinaryAsync(byte[] data, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Attempted to send binary data on a non-open WebSocket.");
                return;
            }
            await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, cancellationToken);
        }

        public async Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning("Attempted to send text data on a non-open WebSocket.");
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(text);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task DisconnectAsync(string reason)
        {
            // Signal the receive loop to stop
            if (!_loopCts.IsCancellationRequested)
            {
                _loopCts.Cancel();
            }

            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, timeout.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception while closing WebSocket output.");
                }
            }
        }

        public void Dispose()
        {
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _webSocket?.Dispose();
        }
    }
}

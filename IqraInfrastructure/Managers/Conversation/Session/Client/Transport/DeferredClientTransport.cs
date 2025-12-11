using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Transport
{
    public class DeferredClientTransport : IConversationClientTransport
    {
        private IConversationClientTransport _actualTransport;
        private readonly ILogger _logger;
        private readonly object _lock = new object();
        private bool _isActivated = false;

        // Events must be declared to satisfy the interface
        public event EventHandler<byte[]> BinaryMessageReceived;
        public event EventHandler<string> TextMessageReceived;
        public event EventHandler<string> Disconnected;

        public bool IsActivated => _isActivated;
        public Type TraspontType => _actualTransport.GetType();

        public DeferredClientTransport(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Activates this deferred transport by providing the real, underlying transport.
        /// This method wires up the events and makes the transport operational.
        /// </summary>
        /// <param name="actualTransport">The concrete transport to use (e.g., WebSocketClientTransport).</param>
        public void Activate(IConversationClientTransport actualTransport)
        {
            lock (_lock)
            {
                if (_isActivated)
                {
                    _logger.LogWarning("Deferred transport was already activated. The new transport will be ignored.");
                    return;
                }

                _logger.LogInformation("Activating deferred transport with a real transport of type {TransportType}.", actualTransport.GetType().Name);
                _actualTransport = actualTransport ?? throw new ArgumentNullException(nameof(actualTransport));

                // Wire up the events from the actual transport to be re-raised by this one.
                _actualTransport.BinaryMessageReceived += (sender, data) => BinaryMessageReceived?.Invoke(this, data);
                _actualTransport.TextMessageReceived += (sender, text) => TextMessageReceived?.Invoke(this, text);
                _actualTransport.Disconnected += (sender, reason) => Disconnected?.Invoke(this, reason);

                _isActivated = true;
            }
        }

        public Task SendBinaryAsync(byte[] data, int sampleRate, int bitsPerSample, int frameDurationMs, CancellationToken cancellationToken)
        {
            if (!_isActivated)
            {
                _logger.LogError("Attempted to send binary data before transport was activated.");
                throw new InvalidOperationException("Transport is not yet active.");
            }
            return _actualTransport.SendBinaryAsync(data, sampleRate, bitsPerSample, frameDurationMs, cancellationToken);
        }

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            if (!_isActivated)
            {
                _logger.LogError("Attempted to send text data before transport was activated.");
                throw new InvalidOperationException("Transport is not yet active.");
            }
            return _actualTransport.SendTextAsync(text, cancellationToken);
        }

        public Task DisconnectAsync(string reason)
        {
            if (!_isActivated)
            {
                _logger.LogWarning("Disconnect called on a non-activated transport. Nothing to do.");
                return Task.CompletedTask;
            }
            return _actualTransport.DisconnectAsync(reason);
        }

        public void Dispose()
        {
            _actualTransport?.Dispose();
        }
    }
}

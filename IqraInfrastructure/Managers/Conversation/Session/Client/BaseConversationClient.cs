using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public abstract class BaseConversationClient : IConversationClient
    {
        public IConversationClientTransport Transport { get; protected set; }
        protected readonly ILogger _logger;
        private bool _hasDisconnected = false;

        public string ClientId { get; }
        public abstract ConversationClientType ClientType { get; }

        public event EventHandler<ConversationAudioReceivedEventArgs> AudioReceived;
        public event EventHandler<ConversationTextReceivedEventArgs> TextReceived;
        public event EventHandler<ConversationClientDisconnectedEventArgs> Disconnected;

        protected BaseConversationClient(string clientId, IConversationClientTransport transport, ILogger logger)
        {
            ClientId = clientId;
            Transport = transport; // Use the public property
            _logger = logger;

            Transport.BinaryMessageReceived += OnTransportBinaryMessageReceived;
            Transport.TextMessageReceived += OnTransportTextMessageReceived;
            Transport.Disconnected += OnTransportDisconnected;
        }

        // Abstract handlers for subclasses to implement protocol-specific logic
        protected abstract void OnTransportBinaryMessageReceived(object sender, byte[] data);
        protected abstract void OnTransportTextMessageReceived(object sender, string message);

        protected virtual void OnTransportDisconnected(object sender, string reason)
        {
            if (_hasDisconnected) return;
            _hasDisconnected = true;

            _logger.LogInformation("Client {ClientId} transport disconnected. Reason: {Reason}", ClientId, reason);
            Disconnected?.Invoke(this, new ConversationClientDisconnectedEventArgs(reason));
        }

        // Implement IConversationClient methods by delegating to the transport
        public abstract Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        public virtual Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            return Transport.SendTextAsync(text, cancellationToken);
        }
        public virtual Task DisconnectAsync(string reason)
        {
            return Transport.DisconnectAsync(reason);
        }

        // Protected event invokers for subclasses to raise the public-facing events
        protected void RaiseAudioReceived(byte[] audioData) => AudioReceived?.Invoke(this, new(audioData));
        protected void RaiseTextReceived(string text) => TextReceived?.Invoke(this, new(text));

        public virtual void Dispose()
        {
            Transport.BinaryMessageReceived -= OnTransportBinaryMessageReceived;
            Transport.TextMessageReceived -= OnTransportTextMessageReceived;
            Transport.Disconnected -= OnTransportDisconnected;
            Transport.Dispose();
        }
    }
}

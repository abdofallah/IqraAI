using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Client
{
    public abstract class BaseTelephonyConversationClient : IConversationClient
    {
        protected readonly string _clientId;
        protected readonly ILogger _logger;
        protected bool _isConnected;
        protected CancellationTokenSource? _connectionCts;
        protected TelephonyProviderEnum _clientTelephonyType;

        public string ClientId => _clientId;
        public ConversationClientType ClientType => ConversationClientType.Telephony;
        public TelephonyProviderEnum ClientTelephonyType => _clientTelephonyType;

        public event EventHandler<ConversationAudioReceivedEventArgs>? AudioReceived;
        public event EventHandler<ConversationTextReceivedEventArgs>? TextReceived;
        public event EventHandler<ConversationClientDisconnectedEventArgs>? Disconnected;

        protected BaseTelephonyConversationClient(string clientId, ILogger logger)
        {
            _clientId = clientId;
            _logger = logger;
            _isConnected = false;
            _clientTelephonyType = TelephonyProviderEnum.Unknown;
        }

        public abstract Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        public abstract Task SendTextAsync(string text, CancellationToken cancellationToken);
        public abstract Task ConnectAsync(CancellationToken cancellationToken);
        public abstract Task DisconnectAsync(string reason);

        protected void OnAudioReceived(byte[] audioData)
        {
            AudioReceived?.Invoke(this, new ConversationAudioReceivedEventArgs(audioData));
        }

        protected void OnTextReceived(string text)
        {
            TextReceived?.Invoke(this, new ConversationTextReceivedEventArgs(text));
        }

        protected void OnDisconnected(string reason)
        {
            _isConnected = false;
            Disconnected?.Invoke(this, new ConversationClientDisconnectedEventArgs(reason));
        }
    }
}
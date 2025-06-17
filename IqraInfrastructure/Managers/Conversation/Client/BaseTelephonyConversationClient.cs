using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Client
{
    public abstract class BaseTelephonyConversationClient : IConversationClient
    {
        protected readonly string _clientId;
        protected readonly string _clientTelephonyPhoneNumber;
        protected readonly string _customerPhoneNumber;
        protected readonly ILogger _logger;
        protected bool _isConnected;
        protected bool _hasDiconnected;
        protected CancellationTokenSource? _connectionCts;
        protected TelephonyProviderEnum _clientTelephonyProviderType;
        protected string _clientTelephonyProviderPhoneNumberId;

        public string ClientId => _clientId;
        public bool IsConnected => _isConnected;
        public bool HasDisconnected => _hasDiconnected;
        public string ClientPhoneNumber => _clientTelephonyPhoneNumber;
        public string CustomerPhoneNumber => _customerPhoneNumber;
        public ConversationClientType ClientType => ConversationClientType.Telephony;
        public TelephonyProviderEnum ClientTelephonyType => _clientTelephonyProviderType;
        public string ClientTelephonyProviderPhoneNumberId => _clientTelephonyProviderPhoneNumberId;

        public event EventHandler<ConversationAudioReceivedEventArgs>? AudioReceived;
        public event EventHandler<ConversationTextReceivedEventArgs>? TextReceived;
        public event EventHandler<ConversationClientDisconnectedEventArgs>? Disconnected;
        public event EventHandler<ConversationDTMFReceivedEventArgs>? DTMFReceived;

        protected BaseTelephonyConversationClient(string clientId, string telephonyPhoneNumber, string telephonyProviderPhoneNumberId, string customerPhoneNumber, ILogger logger)
        {
            _clientId = clientId;
            _clientTelephonyPhoneNumber = telephonyPhoneNumber;
            _clientTelephonyProviderPhoneNumberId = telephonyProviderPhoneNumberId;
            _customerPhoneNumber = customerPhoneNumber;
            _logger = logger;
            _isConnected = false;
            _hasDiconnected = false;
            _clientTelephonyProviderType = TelephonyProviderEnum.Unknown;
        }

        public abstract Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        public abstract Task SendDTMFAsync(string digits, CancellationToken cancellationToken);
        public abstract Task DisconnectAsync(string reason);

        protected void OnAudioReceived(byte[] audioData)
        {
            AudioReceived?.Invoke(this, new ConversationAudioReceivedEventArgs(audioData));
        }

        protected void OnTextReceived(string text)
        {
            TextReceived?.Invoke(this, new ConversationTextReceivedEventArgs(text));
        }

        protected void OnDTMFRecieved(string digit)
        {
            DTMFReceived?.Invoke(this, new ConversationDTMFReceivedEventArgs(digit));
        }

        protected void OnDisconnected(string reason)
        {
            _isConnected = false;
            Disconnected?.Invoke(this, new ConversationClientDisconnectedEventArgs(reason));
        }

        public abstract void Dispose();

        public Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            // telephony client cant display text so we ignore it
            // should never be called for telephony clients in the first place
            return Task.CompletedTask;
        }
    }
}
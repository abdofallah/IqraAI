using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client.Telephony
{
    public abstract class BaseTelephonyConversationClient : BaseConversationClient
    {
        public string ClientTelephonyPhoneNumber { get; }
        public string CustomerPhoneNumber { get; }
        public TelephonyProviderEnum ClientTelephonyProviderType { get; protected set; }
        public string ClientTelephonyProviderPhoneNumberId { get; }

        public override ConversationClientType ClientType => ConversationClientType.Telephony;
        public event EventHandler<ConversationDTMFReceivedEventArgs> DTMFReceived;

        protected BaseTelephonyConversationClient(
            string clientId,
            string telephonyPhoneNumber,
            string telephonyProviderPhoneNumberId,
            string customerPhoneNumber,
            IConversationClientTransport transport,
            ILogger logger
        ) : base(clientId, transport, logger)
        {
            ClientTelephonyPhoneNumber = telephonyPhoneNumber;
            ClientTelephonyProviderPhoneNumberId = telephonyProviderPhoneNumberId;
            CustomerPhoneNumber = customerPhoneNumber;
            ClientTelephonyProviderType = TelephonyProviderEnum.Unknown;
        }

        public abstract Task SendDTMFAsync(string digits, CancellationToken cancellationToken);

        /// <summary>
        /// Telephony clients typically cannot render text. This implementation prevents sending text.
        /// </summary>
        public override Task SendTextAsync(string text, CancellationToken cancellationToken)
        {
            _logger.LogWarning("SendTextAsync was called for telephony client {ClientId}, which is not supported. Ignoring.", ClientId);
            return Task.CompletedTask;
        }

        protected void RaiseDTMFReceived(string digit) => DTMFReceived?.Invoke(this, new(digit));
    }
}
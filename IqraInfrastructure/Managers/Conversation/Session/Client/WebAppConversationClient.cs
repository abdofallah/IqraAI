using IqraCore.Entities.Conversation.Enum;
using IqraCore.Interfaces.Conversation;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Conversation.Session.Client
{
    public class WebAppConversationClient : BaseConversationClient
    {
        public override ConversationClientType ClientType => ConversationClientType.Web;

        public WebAppConversationClient(
            string clientId,
            IConversationClientTransport transport, // This will be an instance of WebRtcClientTransport
            ILogger<WebAppConversationClient> logger
        ) : base(clientId, transport, logger) { }

        /// <summary>
        /// For a WebRTC client, a binary message from the transport is audio from the user.
        /// </summary>
        protected override void OnTransportBinaryMessageReceived(object sender, byte[] data)
        {
            RaiseAudioReceived(data);
        }

        /// <summary>
        /// For a WebRTC client, a text message from the transport is a chat message from the user's browser.
        /// </summary>
        protected override void OnTransportTextMessageReceived(object sender, string message)
        {
            RaiseTextReceived(message);
        }

        /// <summary>
        /// Sends audio to the user by passing it to the transport, which will handle RTP packetization.
        /// </summary>
        public override Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken)
        {
            return Transport.SendBinaryAsync(audioData, cancellationToken);
        }

        // The base class implementation of SendTextAsync already calls Transport.SendTextAsync,
        // so we don't even need to override it. It works perfectly.
    }
}

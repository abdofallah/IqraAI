using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;
using IqraCore.Entities.Helpers;

namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationClient : IDisposable
    {
        string ClientId { get; }
        ConversationClientType ClientType { get; }

        Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        Task SendTextAsync(string text, CancellationToken cancellationToken);
        Task DisconnectAsync(string reason);

        event EventHandler<ConversationAudioReceivedEventArgs> AudioReceived;
        event EventHandler<ConversationTextReceivedEventArgs> TextReceived;
        event EventHandler<ConversationClientDisconnectedEventArgs> Disconnected;
    }
}
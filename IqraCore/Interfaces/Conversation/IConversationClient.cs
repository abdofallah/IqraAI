using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;

namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationClient : IDisposable
    {
        string ClientId { get; }
        ConversationClientConfiguration ClientConfig { get; }
        ConversationClientType ClientType { get; }

        Task ProcessDownstreamAudioAsync(ReadOnlyMemory<byte> masterAudioData, int masterSampleRate, int masterBitsPerSample);

        Task SendAudioAsync(byte[] audioData, CancellationToken cancellationToken);
        Task SendTextAsync(string text, CancellationToken cancellationToken);
        Task DisconnectAsync(string reason);

        event EventHandler<ConversationAudioReceivedEventArgs> AudioReceived;
        event EventHandler<ConversationTextReceivedEventArgs> TextReceived;
        event EventHandler<ConversationClientDisconnectedEventArgs> Disconnected;
    }
}
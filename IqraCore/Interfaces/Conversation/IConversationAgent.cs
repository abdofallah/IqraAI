using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;

namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationAgent : IDisposable
    {
        string AgentId { get; }
        ConversationAgentConfiguration AgentConfiguration { get; }
        ConversationAgentType AgentType { get; }

        Task NotifyConversationStarted();
        Task NotifyMaxDurationReached();
        Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken);
        Task ProcessTextAsync(string text, string clientId, CancellationToken cancellationToken);
        Task ProcessDTMFAsync(string digit, string clientId, CancellationToken cancellationToken);
        Task InitializeAsync();
        Task UpdateOutputFormatAsync(int sampleRate, int bitsPerSample);
        Task ShutdownAsync(string reason);

        event EventHandler<ConversationAudioGeneratedEventArgs> AudioGenerated;

        event EventHandler<object?>? ClearBufferedAudio;

        event EventHandler<ConversationAgentThinkingEventArgs> Thinking;
        event EventHandler<ConversationAgentErrorEventArgs> ErrorOccurred;
    }
}
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;

namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationAgent
    {
        string AgentId { get; }
        ConversationAgentType AgentType { get; }

        Task ProcessAudioAsync(byte[] audioData, string clientId, CancellationToken cancellationToken);
        Task ProcessTextAsync(string text, string clientId, CancellationToken cancellationToken);
        Task InitializeAsync(ConversationAgentConfiguration config, CancellationToken cancellationToken);
        Task ShutdownAsync(string reason);

        event EventHandler<ConversationAudioGeneratedEventArgs> AudioGenerated;
        event EventHandler<ConversationTextGeneratedEventArgs> TextGenerated;
        event EventHandler<ConversationAgentThinkingEventArgs> Thinking;
        event EventHandler<ConversationAgentErrorEventArgs> ErrorOccurred;
    }
}
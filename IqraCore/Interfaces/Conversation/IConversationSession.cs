using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Configuration;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Events;

namespace IqraCore.Interfaces.Conversation
{
    public interface IConversationSession
    {
        string SessionId { get; }
        ConversationSessionConfiguration Configuration { get; }

        Task<bool> AddClientAsync(IConversationClient client);
        Task<bool> RemoveClientAsync(string clientId, string reason);
        IReadOnlyList<IConversationClient> GetClients();
        Task<bool> AddAgentAsync(IConversationAgent agent, ConversationAgentConfiguration configuration);
        Task<bool> RemoveAgentAsync(string agentId, string reason);
        IReadOnlyList<IConversationAgent> GetAgents();
        Task StartAsync(CancellationToken cancellationToken);
        Task PauseAsync(string reason);
        Task ResumeAsync();
        Task EndAsync(string reason);
        IReadOnlyList<ConversationMessage> GetHistory();

        void AddLogEntry(ConversationLogLevel level, string message, object data = null);

        event EventHandler<ConversationSessionStateChangedEventArgs> StateChanged;
        event EventHandler<ConversationMessageAddedEventArgs> MessageAdded;
        event EventHandler<ConversationClientAddedEventArgs> ClientAdded;
        event EventHandler<ConversationClientRemovedEventArgs> ClientRemoved;
        event EventHandler<ConversationAgentAddedEventArgs> AgentAdded;
        event EventHandler<ConversationAgentRemovedEventArgs> AgentRemoved;
    } 
}
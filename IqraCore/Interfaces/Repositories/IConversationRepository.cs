using IqraCore.Entities;

namespace IqraCore.Interfaces.Repositories
{
    public interface IConversationRepository
    {
        Task AddSessionConversationAsync(SessionConversation conversation);
        Task<SessionConversation> GetSessionConversation(string sessionId);
        Task<bool> AddChatToConversationList(string sessionId, ConversationData conversationData);
    }
}
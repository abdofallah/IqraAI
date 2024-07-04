using IqraCore.Entities.Conversation;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IConversationRepository
    {
        Task<List<ConversationData>> GetConversationsAsync();
        Task<List<ConversationData>> GetBusinessConversationsAsync(long businessId);
        Task<List<ConversationData>> GetBusinessConversationsAsync(List<long> sessionsId);
        Task<ConversationData> GetConversationAsync(long sessionId);
        Task AddConversationAsync(ConversationData conversation);
        Task<bool> DeleteConversationAsync(long sessionId);
        Task<bool> UpdateConversationAsync(long sessionId, UpdateDefinition<ConversationData> updateDefinition);
    }
}
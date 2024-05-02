using IqraCore.Entities;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly IMongoCollection<SessionConversation> _conversationsCollection;

        public ConversationRepository(IMongoDatabase database)
        {
            _conversationsCollection = database.GetCollection<SessionConversation>("conversations");
        }

        public async Task AddSessionConversationAsync(SessionConversation conversation)
        {
            await _conversationsCollection.InsertOneAsync(conversation);
        }

        public async Task<SessionConversation> GetSessionConversation(string sessionId)
        {
            var filter = Builders<SessionConversation>.Filter.Eq(c => c.SessionId, sessionId);
            return await _conversationsCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> AddChatToConversationList(string sessionId, ConversationData conversationData)
        {
            var update = Builders<SessionConversation>.Update
                .Push(c => c.ConversationList, conversationData);

            var result = await _conversationsCollection
                .UpdateOneAsync(c => c.SessionId == sessionId, update);

            return result.ModifiedCount > 0;
        }
    }
}
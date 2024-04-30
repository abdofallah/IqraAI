using System.Collections.Generic;
using System.Threading.Tasks;
using IqraCore.Entities.Session.Conversation;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly IMongoCollection<Conversation> _conversationsCollection;

        public ConversationRepository(IMongoDatabase database)
        {
            _conversationsCollection = database.GetCollection<Conversation>("conversations");
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _conversationsCollection.InsertOneAsync(conversation);
        }

        public async Task<IEnumerable<Conversation>> GetConversationsBySessionAsync(string sessionId)
        {
            var filter = Builders<Conversation>.Filter.Eq(c => c.SessionId, sessionId);
            return await _conversationsCollection.Find(filter).ToListAsync();
        }
    }
}
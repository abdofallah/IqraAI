using IqraCore.Entities.Conversation;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly string CollectionName = "Conversations";
        private readonly IMongoCollection<ConversationData> _conversationsCollection;

        public ConversationRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _conversationsCollection = database.GetCollection<ConversationData>(CollectionName);
        }
        public ConversationRepository(IMongoDatabase database)
        {
            _conversationsCollection = database.GetCollection<ConversationData>(CollectionName);
        }

        public Task AddConversationAsync(ConversationData conversation)
        {
            return _conversationsCollection.InsertOneAsync(conversation);
        }

        public async Task<bool> DeleteConversationAsync(long sessionId)
        {
            var filter = Builders<ConversationData>.Filter.Eq(b => b.Id, sessionId);
            var result = await _conversationsCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public Task<List<ConversationData>> GetBusinessConversationsAsync(long businessId)
        {
            var filter = Builders<ConversationData>.Filter.Eq(b => b.BusinessId, businessId);
            return _conversationsCollection.Find(filter).ToListAsync();
        }

        public Task<List<ConversationData>> GetBusinessConversationsAsync(List<long> sessionsId)
        {
            var filter = Builders<ConversationData>.Filter.In(b => b.Id, sessionsId);
            return _conversationsCollection.Find(filter).ToListAsync();
        }

        public Task<ConversationData> GetConversationAsync(long sessionId)
        {
            var filter = Builders<ConversationData>.Filter.Eq(b => b.Id, sessionId);
            return _conversationsCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task<List<ConversationData>> GetConversationsAsync()
        {
            return _conversationsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<bool> UpdateConversationAsync(long sessionId, UpdateDefinition<ConversationData> updateDefinition)
        {
            var filter = Builders<ConversationData>.Filter.Eq(b => b.Id, sessionId);
            var result = await _conversationsCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }
    }
}
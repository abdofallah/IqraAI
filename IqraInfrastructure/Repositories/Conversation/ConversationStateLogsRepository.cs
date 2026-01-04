using IqraCore.Entities.Conversation.Logs;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class ConversationStateLogsRepository
    {
        private readonly IMongoCollection<ConversationStateLogsData> _conversationStateLogsCollection;
        private readonly ILogger<ConversationStateLogsRepository> _logger;

        private readonly string DatabaseName = "IqraConversationState";
        private readonly string CollectionName = "ConversationStatesLogs";

        public ConversationStateLogsRepository(IMongoClient client, ILogger<ConversationStateLogsRepository> logger)
        {
            _logger = logger;

            var database = client.GetDatabase(DatabaseName);
            _conversationStateLogsCollection = database.GetCollection<ConversationStateLogsData>(CollectionName);

            var indexKeysDefinition = Builders<ConversationStateLogsData>.IndexKeys.Ascending(c => c.Id);
            _conversationStateLogsCollection.Indexes.CreateOne(new CreateIndexModel<ConversationStateLogsData>(indexKeysDefinition));
        }

        public async Task<bool> AddLogEntryAsync(string conversationId, ConversationStateLogEntry logEntry)
        {
            try
            {
                var filter = Builders<ConversationStateLogsData>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationStateLogsData>.Update
                    .Push(c => c.Logs, logEntry)
                    .SetOnInsert(c => c.Id, conversationId);

                var options = new UpdateOptions { IsUpsert = true };

                var result = await _conversationStateLogsCollection.UpdateOneAsync(filter, update, options);

                return result.IsAcknowledged && (result.MatchedCount > 0 || result.UpsertedId != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log entry to conversation logs data {Id}", conversationId);
                throw;
            }
        }
    }
}

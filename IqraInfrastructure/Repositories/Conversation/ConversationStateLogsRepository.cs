using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Conversation.Logs.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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

        /// <summary>
        /// Retrieves paginated log entries for a conversation session using MongoDB aggregation.
        /// Supports text search and log level filtering with server-side pagination.
        /// </summary>
        public async Task<(List<ConversationStateLogEntry> Logs, long TotalCount)> GetLogsPaginatedAsync(
            string sessionId,
            int skip,
            int limit,
            string? searchQuery = null,
            ConversationStateLogLevelEnum? levelFilter = null)
        {
            try
            {
                var pipeline = new List<BsonDocument>();

                // Stage 1: Match the document by session ID
                pipeline.Add(new BsonDocument("$match", new BsonDocument("_id", sessionId)));

                // Stage 2: Unwind the Logs array
                pipeline.Add(new BsonDocument("$unwind", "$Logs"));

                // Stage 3: Optional search filter on message
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    pipeline.Add(new BsonDocument("$match", new BsonDocument("Logs.Message",
                        new BsonDocument("$regex", searchQuery).Add("$options", "i"))));
                }

                // Stage 4: Optional level filter
                if (levelFilter.HasValue)
                {
                    pipeline.Add(new BsonDocument("$match", new BsonDocument("Logs.Level", (int)levelFilter.Value)));
                }

                // Stage 5: Sort by timestamp descending (newest first)
                pipeline.Add(new BsonDocument("$sort", new BsonDocument("Logs.Timestamp", -1)));

                // Stage 6: Facet for pagination + total count
                pipeline.Add(new BsonDocument("$facet", new BsonDocument
                {
                    { "data", new BsonArray
                        {
                            new BsonDocument("$skip", skip),
                            new BsonDocument("$limit", limit),
                            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$Logs"))
                        }
                    },
                    { "totalCount", new BsonArray
                        {
                            new BsonDocument("$count", "count")
                        }
                    }
                }));

                var result = await _conversationStateLogsCollection
                    .Aggregate<BsonDocument>(pipeline)
                    .FirstOrDefaultAsync();

                if (result == null)
                {
                    return (new List<ConversationStateLogEntry>(), 0);
                }

                var dataArray = result["data"].AsBsonArray;
                var totalCountArray = result["totalCount"].AsBsonArray;

                long totalCount = totalCountArray.Count > 0
                    ? totalCountArray[0]["count"].AsInt32
                    : 0;

                var logs = new List<ConversationStateLogEntry>();
                foreach (var item in dataArray)
                {
                    var doc = item.AsBsonDocument;
                    var logEntry = new ConversationStateLogEntry
                    {
                        Id = doc.Contains("Id") ? doc["Id"].AsString : doc.Contains("_id") ? doc["_id"].AsString : "",
                        SenderType = doc.Contains("SenderType") ? (ConversationStateLogSenderTypeEnum)doc["SenderType"].AsInt32 : ConversationStateLogSenderTypeEnum.Unknown,
                        Level = doc.Contains("Level") ? (ConversationStateLogLevelEnum)doc["Level"].AsInt32 : ConversationStateLogLevelEnum.Information,
                        Message = doc.Contains("Message") ? doc["Message"].AsString : "",
                        Timestamp = doc.Contains("Timestamp") ? doc["Timestamp"].ToUniversalTime() : DateTime.UtcNow,
                        SystemSenderReference = doc.Contains("SystemSenderReference") && !doc["SystemSenderReference"].IsBsonNull ? doc["SystemSenderReference"].AsString : null,
                        ExceptionDataJson = doc.Contains("ExceptionDataJson") && !doc["ExceptionDataJson"].IsBsonNull ? doc["ExceptionDataJson"].AsString : null,
                    };
                    logs.Add(logEntry);
                }

                return (logs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated logs for conversation {SessionId}", sessionId);
                return (new List<ConversationStateLogEntry>(), 0);
            }
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

using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class ConversationStateRepository
    {
        private readonly IMongoCollection<ConversationState> _conversationStateCollection;
        private readonly ILogger<ConversationStateRepository> _logger;

        public ConversationStateRepository(
            string connectionString,
            string databaseName,
            ILogger<ConversationStateRepository> logger)
        {
            _logger = logger;

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _conversationStateCollection = database.GetCollection<ConversationState>("ConversationStates");

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            try
            {
                // Create index for business ID
                var businessIdIndex = Builders<ConversationState>.IndexKeys.Ascending(c => c.BusinessId);
                _conversationStateCollection.Indexes.CreateOne(new CreateIndexModel<ConversationState>(businessIdIndex));

                // Create index for queue ID
                var queueIdIndex = Builders<ConversationState>.IndexKeys.Ascending(c => c.QueueId);
                _conversationStateCollection.Indexes.CreateOne(new CreateIndexModel<ConversationState>(queueIdIndex));

                // Create index for status and start time
                var statusIndex = Builders<ConversationState>.IndexKeys
                    .Ascending(c => c.Status)
                    .Descending(c => c.StartTime);
                _conversationStateCollection.Indexes.CreateOne(new CreateIndexModel<ConversationState>(statusIndex));

                // Create TTL index for ended conversations
                var ttlIndex = Builders<ConversationState>.IndexKeys.Ascending(c => c.EndTime);
                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30) };
                _conversationStateCollection.Indexes.CreateOne(new CreateIndexModel<ConversationState>(ttlIndex, indexOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for conversation state collection");
            }
        }

        public async Task<string> CreateAsync(ConversationState conversationState, CancellationToken cancellationToken = default)
        {
            try
            {
                await _conversationStateCollection.InsertOneAsync(conversationState, null, cancellationToken);
                _logger.LogInformation("Created conversation state with ID {Id}", conversationState.Id);
                return conversationState.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation state");
                throw;
            }
        }

        public async Task<ConversationState> GetByIdAsync(string conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                return await _conversationStateCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation state by ID {Id}", conversationId);
                throw;
            }
        }

        public async Task<ConversationState> GetByQueueIdAsync(string queueId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.QueueId, queueId);
                return await _conversationStateCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation state by queue ID {QueueId}", queueId);
                throw;
            }
        }

        public async Task<Dictionary<string, ConversationState>> GetByQueueIdsAsync(IEnumerable<string> queueIds, CancellationToken cancellationToken = default)
        {
            if (queueIds == null || !queueIds.Any())
            {
                return new Dictionary<string, ConversationState>();
            }

            try
            {
                var filter = Builders<ConversationState>.Filter.In(c => c.QueueId, queueIds);
                var states = await _conversationStateCollection.Find(filter).ToListAsync(cancellationToken);

                // Ensure QueueId is unique per state if that's the design, otherwise handle potential duplicates if needed.
                // Assuming QueueId uniquely identifies a conversation state here.
                return states.ToDictionary(s => s.QueueId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation states by queue IDs");
                // Depending on requirements, you might return an empty dictionary or throw
                return new Dictionary<string, ConversationState>();
            }
        }

        public async Task<bool> UpdateAsync(ConversationState conversationState, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationState.Id);
                var result = await _conversationStateCollection.ReplaceOneAsync(filter, conversationState, new ReplaceOptions { IsUpsert = false }, cancellationToken);

                _logger.LogInformation("Updated conversation state with ID {Id}", conversationState.Id);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation state with ID {Id}", conversationState.Id);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(string conversationId, ConversationSessionState status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Set(c => c.Status, status)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                if (status == ConversationSessionState.Ended)
                {
                    update = update.Set(c => c.EndTime, DateTime.UtcNow);
                }

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Updated status of conversation {Id} to {Status}", conversationId, status);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status of conversation {Id} to {Status}", conversationId, status);
                throw;
            }
        }

        public async Task<bool> AddMessageAsync(string conversationId, ConversationMessageData message, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Push(c => c.Messages, message)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                // Update metrics
                if (message.Role == ConversationSenderRole.Client)
                {
                    update = update.Inc(c => c.Metrics.ClientMessageCount, 1);
                    update = update.Inc(c => c.Metrics.ClientWordCount, CountWords(message.Content));
                }
                else if (message.Role == ConversationSenderRole.Agent)
                {
                    update = update.Inc(c => c.Metrics.AgentMessageCount, 1);
                    update = update.Inc(c => c.Metrics.AgentWordCount, CountWords(message.Content));
                }

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogDebug("Added message to conversation {Id}", conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message to conversation {Id}", conversationId);
                throw;
            }
        }

        public async Task<bool> AddLogEntryAsync(string conversationId, ConversationLogEntry logEntry, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Push(c => c.Logs, logEntry);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log entry to conversation {Id}", conversationId);
                throw;
            }
        }

        public async Task<bool> AddClientInfoAsync(string conversationId, ConversationClientInfo clientInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Push(c => c.Clients, clientInfo)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Added client {ClientId} to conversation {Id}", clientInfo.ClientId, conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding client {ClientId} to conversation {Id}", clientInfo.ClientId, conversationId);
                throw;
            }
        }

        public async Task<bool> UpdateClientLeftAsync(string conversationId, string clientId, DateTime leftAt, string leaveReason, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.ElemMatch(c => c.Clients, client => client.ClientId == clientId)
                );

                var update = Builders<ConversationState>.Update
                    .Set(c => c.Clients.FirstMatchingElement().LeftAt, leftAt)
                    .Set(c => c.Clients.FirstMatchingElement().LeaveReason, leaveReason)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Updated client {ClientId} left information in conversation {Id}", clientId, conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client {ClientId} left information in conversation {Id}", clientId, conversationId);
                throw;
            }
        }

        public async Task<bool> AddAgentInfoAsync(string conversationId, ConversationAgentInfo agentInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Push(c => c.Agents, agentInfo)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Added agent {AgentId} to conversation {Id}", agentInfo.AgentId, conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding agent {AgentId} to conversation {Id}", agentInfo.AgentId, conversationId);
                throw;
            }
        }

        public async Task<bool> UpdateAgentLeftAsync(string conversationId, string agentId, DateTime leftAt, string leaveReason, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.ElemMatch(c => c.Agents, agent => agent.AgentId == agentId)
                );

                var update = Builders<ConversationState>.Update
                    .Set(c => c.Agents.FirstMatchingElement().LeftAt, leftAt)
                    .Set(c => c.Agents.FirstMatchingElement().LeaveReason, leaveReason)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Updated agent {AgentId} left information in conversation {Id}", agentId, conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent {AgentId} left information in conversation {Id}", agentId, conversationId);
                throw;
            }
        }

        public async Task<bool> UpdateMetricsAsync(string conversationId, ConversationMetrics metrics, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);
                var update = Builders<ConversationState>.Update
                    .Set(c => c.Metrics, metrics)
                    .Set(c => c.LastActivityTime, DateTime.UtcNow);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogInformation("Updated metrics for conversation {Id}", conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating metrics for conversation {Id}", conversationId);
                throw;
            }
        }

        public async Task<bool> SetClientAudioStatusAsync(string conversationId, string clientId, ConversationMemberAudioCompilationStatus status, string? failedReason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.ElemMatch(c => c.Clients, client => client.ClientId == clientId)
                );

                var update = Builders<ConversationState>.Update
                    .Set(c => c.Clients.FirstMatchingElement().AudioInfo.AudioCompilationStatus, status);
                if (failedReason != null && status == ConversationMemberAudioCompilationStatus.Failed)
                {
                    update = update.Set(c => c.Clients.FirstMatchingElement().AudioInfo.FailedReason, failedReason);
                }

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client {ClientId} audio compilation status in conversation {Id}", clientId, conversationId);
                throw;
            }
        }

        public async Task<bool> SetAgentAudioStatusAsync(string conversationId, string agentId, ConversationMemberAudioCompilationStatus status, string? failedReason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.ElemMatch(c => c.Agents, agent => agent.AgentId == agentId)
                );

                var update = Builders<ConversationState>.Update
                    .Set(c => c.Agents.FirstMatchingElement().AudioInfo.AudioCompilationStatus, status);
                if (failedReason != null && status == ConversationMemberAudioCompilationStatus.Failed)
                {
                    update = update.Set(c => c.Agents.FirstMatchingElement().AudioInfo.FailedReason, failedReason);
                }

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating agent {AgentId} audio compilation status in conversation {Id}", agentId, conversationId);
                throw;
            }
        }

        public async Task<List<ConversationState>> GetRecentForBusinessAsync(long businessId, int limit = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.BusinessId, businessId);
                var sort = Builders<ConversationState>.Sort.Descending(c => c.StartTime);

                return await _conversationStateCollection.Find(filter)
                    .Sort(sort)
                    .Limit(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent conversations for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<List<ConversationState>> GetActiveForBusinessAsync(long businessId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.BusinessId, businessId),
                    Builders<ConversationState>.Filter.In(c => c.Status, new[]
                    {
                        ConversationSessionState.Active,
                        ConversationSessionState.Starting,
                        ConversationSessionState.Paused
                    })
                );

                var sort = Builders<ConversationState>.Sort.Descending(c => c.StartTime);

                return await _conversationStateCollection.Find(filter)
                    .Sort(sort)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active conversations for business {BusinessId}", businessId);
                throw;
            }
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
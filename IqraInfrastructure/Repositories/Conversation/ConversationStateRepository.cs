using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Conversations;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class ConversationStateRepository
    {
        private readonly IMongoCollection<ConversationState> _conversationStateCollection;
        private readonly ILogger<ConversationStateRepository> _logger;

        public ConversationStateRepository(IMongoClient client, string databaseName, ILogger<ConversationStateRepository> logger)
        {
            _logger = logger;

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

        public async Task<ConversationState> GetByIdAsync(long businessId, string conversationId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.Eq(c => c.BusinessId, businessId)
                );

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

                return states
                    .GroupBy(s => s.QueueId)
                    .ToDictionary(g => g.Key, g => g.First());
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
                    .Set(c => c.Status, status);

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

        public async Task<bool> StartNewTurnAsync(string conversationId, ConversationTurn turn, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId);

                var arraySizeExpression = Builders<ConversationState>.Update.Push(c => c.Turns, turn);
                var update = Builders<ConversationState>.Update.Push(c => c.Turns, turn);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                _logger.LogDebug("Started new turn {TurnId} with sequence {Sequence} in conversation {ConversationId}", turn.Id, turn.Sequence, conversationId);
                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new turn in conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> UpdateTurnAsync(string conversationId, ConversationTurn turn, CancellationToken cancellationToken = default)
        {
            try
            {
                // To update an element in an array, we need to match the conversation ID and the turn ID within the array.
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    Builders<ConversationState>.Filter.ElemMatch(c => c.Turns, t => t.Id == turn.Id)
                );

                // The '$' positional operator updates the first element that matched the filter.
                var update = Builders<ConversationState>.Update.Set(c => c.Turns.FirstMatchingElement(), turn);

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                if (result.ModifiedCount == 0)
                {
                    _logger.LogWarning("UpdateTurnAsync for conversation {ConversationId} and turn {TurnId} did not modify any documents. The turn might not exist.", conversationId, turn.Id);
                }
                else
                {
                    _logger.LogTrace("Updated turn {TurnId} in conversation {ConversationId}", turn.Id, conversationId);
                }

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating turn {TurnId} in conversation {ConversationId}", turn.Id, conversationId);
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
                    .Push(c => c.Clients, clientInfo);

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
                    .Set(c => c.Clients.FirstMatchingElement().LeaveReason, leaveReason);

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
                    .Push(c => c.Agents, agentInfo);

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
                    .Set(c => c.Agents.FirstMatchingElement().LeaveReason, leaveReason);

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
                    .Set(c => c.Metrics, metrics);

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

        public async Task<bool> SetMemberAudioStatusAsync(string conversationId, string memberId, ConversationMemberAudioCompilationStatus status, bool isAgent, string? failedReason = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.Id, conversationId),
                    (
                        isAgent == true ?
                        Builders<ConversationState>.Filter.ElemMatch(c => c.Agents, agent => agent.AgentId == memberId)
                            :
                        Builders<ConversationState>.Filter.ElemMatch(c => c.Clients, client => client.ClientId == memberId)
                    )
                );

                var update = isAgent == true?
                    Builders<ConversationState>.Update
                    .Set(c => c.Agents.FirstMatchingElement().AudioInfo.AudioCompilationStatus, status)
                    :
                    Builders<ConversationState>.Update
                    .Set(c => c.Clients.FirstMatchingElement().AudioInfo.AudioCompilationStatus, status);

                if (failedReason != null && status == ConversationMemberAudioCompilationStatus.Failed)
                {
                    if (isAgent)
                    {
                        update = update.Set(c => c.Agents.FirstMatchingElement().AudioInfo.FailedReason, failedReason);
                    }
                    else
                    {
                        update = update.Set(c => c.Clients.FirstMatchingElement().AudioInfo.FailedReason, failedReason);
                    }
                }

                var result = await _conversationStateCollection.UpdateOneAsync(filter, update, null, cancellationToken);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client {ClientId} audio compilation status in conversation {Id}", memberId, conversationId);
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

        public async Task<long> GetActiveCallCountForBusinessAsync(long businessId, CancellationToken cancellationToken = default)
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

                return await _conversationStateCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error getting active call count for business {BusinessId}", businessId);
                throw;
            }
        }

        public async Task<long> GetActiveSessionsCountByMasterUserEmailAsync(string userEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.BusinessMasterEmail, userEmail),
                    Builders<ConversationState>.Filter.In(c => c.Status, new[]
                    {
                        ConversationSessionState.Active,
                        ConversationSessionState.Starting,
                        ConversationSessionState.Paused
                    })
                );

                return await _conversationStateCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active call count for master user {UserEmail}", userEmail);
                throw;
            }
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public async Task<long> GetActiveCallCountForServerAsync(string serverId, string regionId)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.ProcessingServerId, serverId),
                    Builders<ConversationState>.Filter.Eq(c => c.RegionId, regionId),
                    Builders<ConversationState>.Filter.In(c => c.Status, new[]
                    {
                        ConversationSessionState.Active,
                        ConversationSessionState.Starting,
                        ConversationSessionState.Paused,
                        ConversationSessionState.Ending
                    })
                );

                return await _conversationStateCollection.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active call count for server {ServerId} in region {RegionId}", serverId, regionId);
                throw;
            }
        }

        public async Task<int> CleanupMaxDurationReachedConversationsAsync(string serverId, string regionId, DateTime thresholdTime)
        {
            try
            {
                var filter = Builders<ConversationState>.Filter.And(
                    Builders<ConversationState>.Filter.Eq(c => c.ProcessingServerId, serverId),
                    Builders<ConversationState>.Filter.Eq(c => c.RegionId, regionId),
                    Builders<ConversationState>.Filter.Lt(c => c.ExpectedEndTimeAt, thresholdTime)
                );

                var update = Builders<ConversationState>.Update
                    .Set(c => c.Status, ConversationSessionState.Ended)
                    .Set(c => c.EndTime, DateTime.UtcNow)
                    .Push(c => c.Logs, new ConversationLogEntry() { Level = ConversationLogLevel.Critical, Message = "Expected endtime reached for conversation but it wasnt ended, so manually cleaned up" });

                var result = await _conversationStateCollection.UpdateManyAsync(filter, update);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up max duration reached conversations for server {ServerId} in region {RegionId}", serverId, regionId);
                throw;
            }
        }

        public async Task<(List<ConversationState> Items, bool HasMore, long TotalCount)> GetConversationStatesPaginatedAsync(
            long businessId,
            GetBusinessConversationsRequestFilterModel filter,
            int limit,
            PaginationCursor<GetBusinessConversationsRequestFilterModel>? cursor,
            bool fetchNext)
        {
            try
            {
                var filterBuilder = Builders<ConversationState>.Filter;
                var filterDefinitions = new List<FilterDefinition<ConversationState>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId)
                };

                // --- Build the dynamic filter from the request model ---
                if (filter.StartStartedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.StartTime, filter.StartStartedDate.Value.ToUniversalTime()));
                if (filter.EndStartedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.StartTime, filter.EndStartedDate.Value.ToUniversalTime()));
                if (filter.SessionStates?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, filter.SessionStates));
                if (filter.SessionInitiationTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.SessionInitiationType, filter.SessionInitiationTypes));
                if (filter.SessionEndTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.EndType, filter.SessionEndTypes));

                var baseFilter = filterBuilder.And(filterDefinitions);

                // Get the total count for the filtered results before pagination
                long totalCount = await _conversationStateCollection.CountDocumentsAsync(baseFilter);

                FilterDefinition<ConversationState> finalFilter = baseFilter;
                SortDefinition<ConversationState> sortDefinition;

                if (fetchNext)
                {
                    sortDefinition = Builders<ConversationState>.Sort
                        .Descending(c => c.StartTime)
                        .Descending(c => c.Id); // Use Id for tie-breaking

                    if (cursor != null)
                    {
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Lt(c => c.StartTime, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.StartTime, cursor.Timestamp), filterBuilder.Lt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                }
                else // Fetching Previous Page
                {
                    sortDefinition = Builders<ConversationState>.Sort
                        .Ascending(c => c.StartTime)
                        .Ascending(c => c.Id);

                    if (cursor != null)
                    {
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Gt(c => c.StartTime, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.StartTime, cursor.Timestamp), filterBuilder.Gt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                    else
                    {
                        return (new List<ConversationState>(), false, 0);
                    }
                }

                var items = await _conversationStateCollection.Find(finalFilter)
                    .Sort(sortDefinition)
                    .Limit(limit + 1)
                    .ToListAsync();

                bool hasMore = items.Count > limit;

                if (hasMore)
                {
                    items.RemoveAt(limit);
                }

                if (!fetchNext)
                {
                    items.Reverse();
                }

                return (items, hasMore, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated conversation states for business {BusinessId}", businessId);
                return (new List<ConversationState>(), false, 0);
            }
        }
    }
}
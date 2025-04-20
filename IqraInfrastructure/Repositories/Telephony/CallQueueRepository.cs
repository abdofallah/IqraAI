using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Telephony.Call;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Telephony
{
    public class CallQueueRepository
    {
        private readonly IMongoCollection<CallQueueData> _callQueueCollection;
        private readonly ILogger<CallQueueRepository> _logger;

        public CallQueueRepository(
            string connectionString,
            string databaseName,
            ILogger<CallQueueRepository> logger)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _callQueueCollection = database.GetCollection<CallQueueData>("CallQueue");
            _logger = logger;

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            try
            {
                // Create index for status and enqueued time for efficient querying
                var statusIndex = Builders<CallQueueData>.IndexKeys
                    .Ascending(c => c.Status)
                    .Ascending(c => c.EnqueuedAt);

                _callQueueCollection.Indexes.CreateOne(new CreateIndexModel<CallQueueData>(statusIndex));

                // Create index for business ID
                var businessIndex = Builders<CallQueueData>.IndexKeys
                    .Ascending(c => c.BusinessId)
                    .Ascending(c => c.Status);

                _callQueueCollection.Indexes.CreateOne(new CreateIndexModel<CallQueueData>(businessIndex));

                // Create TTL index to automatically expire completed calls after 24 hours
                var ttlIndex = Builders<CallQueueData>.IndexKeys
                    .Ascending(c => c.CompletedAt);

                var indexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromHours(24) };
                _callQueueCollection.Indexes.CreateOne(new CreateIndexModel<CallQueueData>(ttlIndex, indexOptions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for call queue collection");
            }
        }

        public async Task<string> EnqueueCallAsync(CallQueueData callQueueData)
        {
            try
            {
                await _callQueueCollection.InsertOneAsync(callQueueData);
                _logger.LogInformation("Call enqueued: {CallId} for business {BusinessId}",
                    callQueueData.Id, callQueueData.BusinessId);
                return callQueueData.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing call for business {BusinessId}", callQueueData.BusinessId);
                throw;
            }
        }

        public async Task<List<CallQueueData>> GetNextCallsAsync(int limit, string serverId, string regionId)
        {
            try
            {
                // Find queued calls for this region, ordered by priority and enqueue time
                var filter = Builders<CallQueueData>.Filter.And(
                    Builders<CallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Queued),
                    Builders<CallQueueData>.Filter.Eq(c => c.RegionId, regionId)
                );

                var sort = Builders<CallQueueData>.Sort
                    .Descending(c => c.Priority)
                    .Ascending(c => c.EnqueuedAt);

                var calls = await _callQueueCollection.Find(filter)
                    .Sort(sort)
                    .Limit(limit)
                    .ToListAsync();

                // Mark these calls as processing
                if (calls.Any())
                {
                    var callIds = calls.Select(c => c.Id).ToList();

                    var updateFilter = Builders<CallQueueData>.Filter.In(c => c.Id, callIds);
                    var update = Builders<CallQueueData>.Update
                        .Set(c => c.Status, CallQueueStatusEnum.Processing)
                        .Set(c => c.ProcessingStartedAt, DateTime.UtcNow)
                        .Set(c => c.ProcessingServerId, serverId);

                    await _callQueueCollection.UpdateManyAsync(updateFilter, update);

                    _logger.LogInformation("Retrieved {Count} calls for processing on server {ServerId}",
                        calls.Count, serverId);
                }

                return calls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next calls for server {ServerId}", serverId);
                return new List<CallQueueData>();
            }
        }

        public async Task<CallQueueData?> GetCallByIdAsync(string callId)
        {
            try
            {
                var filter = Builders<CallQueueData>.Filter.Eq(c => c.Id, callId);
                return await _callQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call by ID {CallId}", callId);
                return null;
            }
        }

        public async Task<CallQueueData?> GetCallByProviderCallIdAsync(TelephonyProviderEnum provider, string callId, long businessId, string phoneNumberId)
        {
            try
            {
                var filter =
                    Builders<CallQueueData>.Filter.Eq(c => c.ProviderCallId, callId)
                    &
                    Builders<CallQueueData>.Filter.Eq(c => c.Provider, provider)
                    &
                    Builders<CallQueueData>.Filter.Eq(c => c.BusinessId, businessId)
                    &
                    Builders<CallQueueData>.Filter.Eq(c => c.NumberId, phoneNumberId);

                return await _callQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call by provider call ID {CallId}", callId);
                return null;
            }
        }

        public async Task<(List<CallQueueData> Items, bool HasMore)> GetCallsForBusinessPaginatedAsync(long businessId, int limit, PaginationCursor? cursor, bool fetchNext = true)
        {
            try
            {
                var filterBuilder = Builders<CallQueueData>.Filter;
                var baseFilter = filterBuilder.Eq(c => c.BusinessId, businessId);

                FilterDefinition<CallQueueData> finalFilter = baseFilter;
                SortDefinition<CallQueueData> sortDefinition;

                if (fetchNext)
                {
                    // Sort for fetching the 'next' page (most recent first)
                    sortDefinition = Builders<CallQueueData>.Sort
                        .Descending(c => c.EnqueuedAt)
                        .Descending(c => c.Id); // Use Id for tie-breaking

                    if (cursor != null)
                    {
                        // Apply cursor filter for 'next' page
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Lt(c => c.EnqueuedAt, cursor.Timestamp),
                            filterBuilder.And(
                                filterBuilder.Eq(c => c.EnqueuedAt, cursor.Timestamp),
                                filterBuilder.Lt(c => c.Id, cursor.Id) // MongoDB compares ObjectIds correctly
                            )
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                }
                else // Fetching Previous Page
                {
                    // Sort for fetching the 'previous' page (oldest first temporarily)
                    sortDefinition = Builders<CallQueueData>.Sort
                        .Ascending(c => c.EnqueuedAt)
                        .Ascending(c => c.Id);

                    if (cursor != null)
                    {
                        // Apply cursor filter for 'previous' page
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Gt(c => c.EnqueuedAt, cursor.Timestamp),
                            filterBuilder.And(
                                filterBuilder.Eq(c => c.EnqueuedAt, cursor.Timestamp),
                                filterBuilder.Gt(c => c.Id, cursor.Id)
                            )
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                    else
                    {
                        // Cannot fetch previous page from the very beginning
                        return (new List<CallQueueData>(), false);
                    }
                }

                // Fetch one extra item to determine if there's a next/previous page
                var queryLimit = limit + 1;

                var calls = await _callQueueCollection.Find(finalFilter)
                    .Sort(sortDefinition)
                    .Limit(queryLimit)
                    .ToListAsync();

                bool hasMore = calls.Count > limit;

                // Trim the extra item if it exists
                if (hasMore)
                {
                    calls = calls.Take(limit).ToList();
                }

                // If fetching previous, reverse the results to maintain Descending order for the user
                if (!fetchNext)
                {
                    calls.Reverse();
                }

                return (calls, hasMore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated calls for business {BusinessId}", businessId);
                return (new List<CallQueueData>(), false);
            }
        }

        public async Task UpdateCallSessionIdAsync(string queueId, string sessionId)
        {
            try
            {
                var filter = Builders<CallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<CallQueueData>.Update
                    .Set(c => c.SessionId, sessionId);

                await _callQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session ID for queue {QueueId}", queueId);
            }
        }

        public async Task UpdateCallSessionIdAndStatusAsync(string queueId, string sessionId, CallQueueStatusEnum status)
        {
            try
            {
                var filter = Builders<CallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<CallQueueData>.Update
                    .Set(c => c.SessionId, sessionId)
                    .Set(c => c.Status, status);

                await _callQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session ID and status for queue {QueueId}", queueId);
            }
        }

        public async Task UpdateStatusAsync(string queueId, CallQueueStatusEnum status)
        {
            try
            {
                var filter = Builders<CallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<CallQueueData>.Update
                    .Set(c => c.Status, status);

                await _callQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for queue {QueueId}", queueId);
            }
        }

        public async Task<int> CleanupOrphanedCallsAsync(TimeSpan threshold)
        {
            try
            {
                var thresholdTime = DateTime.UtcNow.Subtract(threshold);

                var filter = Builders<CallQueueData>.Filter.And(
                    Builders<CallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Processing),
                    Builders<CallQueueData>.Filter.Lt(c => c.ProcessingStartedAt, thresholdTime)
                );

                var update = Builders<CallQueueData>.Update
                    .Set(c => c.Status, CallQueueStatusEnum.Failed)
                    .Set(c => c.CompletedAt, DateTime.UtcNow);

                var result = await _callQueueCollection.UpdateManyAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogWarning("Cleaned up {Count} orphaned calls", result.ModifiedCount);
                }

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned calls");
                return 0;
            }
        }

        public async Task<long> GetQueuedCallCountForServerAsync(string serverId, string regionId)
        {
            try
            {
                var filter = Builders<CallQueueData>.Filter.And(
                    Builders<CallQueueData>.Filter.Eq(c => c.ProcessingServerId, serverId),
                    Builders<CallQueueData>.Filter.Eq(c => c.RegionId, regionId),
                    Builders<CallQueueData>.Filter.In(c => c.Status, new[]
                    {
                        CallQueueStatusEnum.Queued
                    })
                );

                return await _callQueueCollection.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued call count for server {ServerId}", serverId);
                return 0;
            }
        }
    }
}
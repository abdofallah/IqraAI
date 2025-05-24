using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Call
{
    public class InboundCallQueueRepository
    {
        private readonly IMongoCollection<InboundCallQueueData> _inboundCallQueueCollection;
        private readonly ILogger<InboundCallQueueRepository> _logger;

        private const string InboundCollectionName = "InboundCallQueue"; 

        public InboundCallQueueRepository(string connectionString, string databaseName, ILogger<InboundCallQueueRepository> logger)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _inboundCallQueueCollection = database.GetCollection<InboundCallQueueData>(InboundCollectionName);

            _logger = logger;

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var inboundIndexes = new[]
            {
                    // For GetInboundCallQueueByProviderCallIdAsync
                    new CreateIndexModel<InboundCallQueueData>(
                        Builders<InboundCallQueueData>.IndexKeys
                            .Ascending(c => c.ProviderCallId)
                            .Ascending(c => c.RouteNumberProvider)
                            .Ascending(c => c.BusinessId)
                            .Ascending(c => c.RouteNumberId),
                        new CreateIndexOptions { Name = "Idx_Inbound_Provider_Business_Route" }),

                    // For GetInboundCallQueuesForBusinessPaginatedAsync
                    new CreateIndexModel<InboundCallQueueData>(
                        Builders<InboundCallQueueData>.IndexKeys
                            .Ascending(c => c.BusinessId)
                            .Descending(c => c.EnqueuedAt) // For default pagination order
                            .Descending(c => c.Id),        // Tie-breaker
                        new CreateIndexOptions { Name = "Idx_Inbound_Business_EnqueuedAt_Id" }),
                };

            _inboundCallQueueCollection.Indexes.CreateManyAsync(inboundIndexes).GetAwaiter().GetResult();
        }

        public async Task<string?> EnqueueInboundCallQueueAsync(InboundCallQueueData inboundCallQueueData)
        {
            try
            {
                await _inboundCallQueueCollection.InsertOneAsync(inboundCallQueueData);
                return inboundCallQueueData.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing inbound call for business {BusinessId}", inboundCallQueueData.BusinessId);
                return null;
            }
        }

        public async Task<InboundCallQueueData?> GetInboundCallQueueByIdAsync(string queueId)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                return await _inboundCallQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbound call queue by ID {QueueId}", queueId);
                return null;
            }
        }

        public async Task<InboundCallQueueData?> GetInboundCallQueueByProviderCallIdAsync(TelephonyProviderEnum provider, string providerCallId, long businessId, string routeNumberId)
        {
            try
            {
                var filterBuilder = Builders<InboundCallQueueData>.Filter;
                var filter = filterBuilder.Eq(c => c.ProviderCallId, providerCallId)
                           & filterBuilder.Eq(c => c.RouteNumberProvider, provider)
                           & filterBuilder.Eq(c => c.BusinessId, businessId)
                           & filterBuilder.Eq(c => c.RouteNumberId, routeNumberId);

                return await _inboundCallQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call by provider call ID {CallId}", providerCallId);
                return null;
            }
        }

        public async Task<(List<InboundCallQueueData> Items, bool HasMore)> GetInboundCallQueuesForBusinessPaginatedAsync(long businessId, int limit, PaginationCursor? cursor, bool fetchNext = true)
        {
            try
            {
                var filterBuilder = Builders<InboundCallQueueData>.Filter;
                var baseFilter = filterBuilder.Eq(c => c.BusinessId, businessId) & filterBuilder.Eq(c => c.Type, CallQueueTypeEnum.Inbound);

                FilterDefinition<InboundCallQueueData> finalFilter = baseFilter;
                SortDefinition<InboundCallQueueData> sortDefinition;

                if (fetchNext)
                {
                    // Sort for fetching the 'next' page (most recent first)
                    sortDefinition = Builders<InboundCallQueueData>.Sort
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
                    sortDefinition = Builders<InboundCallQueueData>.Sort
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
                        return (new List<InboundCallQueueData>(), false);
                    }
                }

                // Fetch one extra item to determine if there's a next/previous page
                var queryLimit = limit + 1;

                var calls = await _inboundCallQueueCollection.Find(finalFilter)
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
                return (new List<InboundCallQueueData>(), false);
            }
        }

        public async Task UpdateInboundCallQueueSessionIdAndStatusAsync(string queueId, string sessionId, CallQueueStatusEnum status)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.SessionId, sessionId)
                    .Set(c => c.Status, status);

                await _inboundCallQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session ID and status for queue {QueueId}", queueId);
            }
        }

        public async Task UpdateInboundCallQueueStatusAsync(string queueId, CallQueueStatusEnum status)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var updateBuilder = Builders<InboundCallQueueData>.Update.Set(c => c.Status, status);

                await _inboundCallQueueCollection.UpdateOneAsync(filter, updateBuilder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for queue {QueueId}", queueId);
            }
        }

        public async Task SetInboundCallQueueFailedStatusAsync(string queueId, CallQueueLog? log = null)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.Status, CallQueueStatusEnum.Failed);
                if (log != null) update = update.AddToSet(c => c.Logs, log);

                await _inboundCallQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating failed status for queue {QueueId}", queueId);
            }
        }

        public async Task UpdateInboundCallQueueProcessingBackendServerIdAsync(string queueId, string? serverId)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.ProcessingBackendServerId, serverId);

                await _inboundCallQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating processing server for queue {QueueId}", queueId);
            }
        }

        public async Task<int> CleanupExpiredInboundCallQueues(string regionId)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.And(
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Queued), // maybe we can add processing and 
                    Builders<InboundCallQueueData>.Filter.Gt(c => c.CreatedAt, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5))), // check if 5minutes is good
                    Builders<InboundCallQueueData>.Filter.Ne(c => c.RegionId, regionId)
                );

                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.Status, CallQueueStatusEnum.Expired)
                    .Set(c => c.CompletedAt, DateTime.UtcNow);

                var result = await _inboundCallQueueCollection.UpdateManyAsync(filter, update);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned calls");
                return 0;
            }
        }

        public async Task<int> CleanupInboundOrphanedCallQueues(string regionId, DateTime thresholdToCheck)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.And(
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Processing),
                    Builders<InboundCallQueueData>.Filter.Lt(c => c.ProcessingStartedAt, thresholdToCheck),
                    Builders<InboundCallQueueData>.Filter.Ne(c => c.RegionId, regionId)
                );

                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.Status, CallQueueStatusEnum.Failed)
                    .Set(c => c.CompletedAt, DateTime.UtcNow);

                var result = await _inboundCallQueueCollection.UpdateManyAsync(filter, update);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned calls");
                return 0;
            }
        }

        public async Task<long> GetActiveInboundCallCountForProcessingServerAsync(string serverId, string regionId)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.And(
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.ProcessingBackendServerId, serverId),
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.RegionId, regionId),
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Queued)
                );

                return await _inboundCallQueueCollection.CountDocumentsAsync(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued call count for server {ServerId}", serverId);
                return 0;
            }
        }
    }
}
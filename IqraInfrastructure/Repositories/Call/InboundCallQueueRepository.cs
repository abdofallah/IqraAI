using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Queues.Inbound;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Repositories.Call
{
    public class InboundCallQueueRepository
    {
        private readonly IMongoCollection<InboundCallQueueData> _inboundCallQueueCollection;
        private readonly ILogger<InboundCallQueueRepository> _logger;
        private readonly CallQueueLogsRepository _callQueueLogsRepository;

        private const string InboundCollectionName = "InboundCallQueue";

        public InboundCallQueueRepository(ILogger<InboundCallQueueRepository> logger, IMongoClient client, string databaseName, CallQueueLogsRepository callQueueLogsRepository)
        {
            _logger = logger;

            var database = client.GetDatabase(databaseName);
            _inboundCallQueueCollection = database.GetCollection<InboundCallQueueData>(InboundCollectionName);

            CreateIndexes();

            _callQueueLogsRepository = callQueueLogsRepository;
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
                            .Descending(c => c.CreatedAt)
                            .Descending(c => c.Id),
                        new CreateIndexOptions { Name = "Idx_Inbound_Business_CreatedAt_Id" })
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

        public async Task<InboundCallQueueData?> GetInboundCallQueueByIdAsync(long businessId, string queueId)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.And(
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId),
                    Builders<InboundCallQueueData>.Filter.Eq(c => c.BusinessId, businessId)
                );
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

        public async Task<(List<InboundCallQueueData> Items, bool HasMore, long TotalCount)> GetInboundCallQueuesForBusinessPaginatedAsync(
    long businessId,
    GetBusinessInboundCallQueuesRequestFilterModel filter,
    int limit,
    PaginationCursor<GetBusinessInboundCallQueuesRequestFilterModel>? cursor,
    bool fetchNext)
        {
            try
            {
                var filterBuilder = Builders<InboundCallQueueData>.Filter;
                var filterDefinitions = new List<FilterDefinition<InboundCallQueueData>>
        {
            filterBuilder.Eq(c => c.BusinessId, businessId),
            filterBuilder.Eq(c => c.Type, CallQueueTypeEnum.Inbound) // Good practice to keep this
        };

                // --- NEW: Dynamically build the filter from the provided model ---
                if (filter.StartCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, filter.StartCreatedDate.Value.ToUniversalTime()));
                if (filter.EndCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, filter.EndCreatedDate.Value.ToUniversalTime()));
                if (filter.StartCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CompletedAt, filter.StartCompletedAtDate.Value.ToUniversalTime()));
                if (filter.EndCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CompletedAt, filter.EndCompletedAtDate.Value.ToUniversalTime()));
                if (filter.QueueStatusTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, filter.QueueStatusTypes));
                if (filter.RouteIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteId, filter.RouteIds));
                if (filter.CallingNumbers?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallerNumber, filter.CallingNumbers));
                if (filter.RouteNumberProviders?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteNumberProvider, filter.RouteNumberProviders));
                if (filter.RouteNumberIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteNumberId, filter.RouteNumberIds));

                var baseFilter = filterBuilder.And(filterDefinitions);

                // NEW: Get the total count for the filtered results before pagination
                long totalCount = await _inboundCallQueueCollection.CountDocumentsAsync(baseFilter);

                FilterDefinition<InboundCallQueueData> finalFilter = baseFilter;
                SortDefinition<InboundCallQueueData> sortDefinition;

                if (fetchNext)
                {
                    sortDefinition = Builders<InboundCallQueueData>.Sort
                        .Descending(c => c.CreatedAt)
                        .Descending(c => c.Id);

                    if (cursor != null)
                    {
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Lt(c => c.CreatedAt, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.CreatedAt, cursor.Timestamp), filterBuilder.Lt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                }
                else // Fetching Previous Page
                {
                    sortDefinition = Builders<InboundCallQueueData>.Sort
                        .Ascending(c => c.CreatedAt)
                        .Ascending(c => c.Id);

                    if (cursor != null)
                    {
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Gt(c => c.CreatedAt, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.CreatedAt, cursor.Timestamp), filterBuilder.Gt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                    else
                    {
                        return (new List<InboundCallQueueData>(), false, 0);
                    }
                }

                var calls = await _inboundCallQueueCollection.Find(finalFilter)
                    .Sort(sortDefinition)
                    .Limit(limit + 1)
                    .ToListAsync();

                bool hasMore = calls.Count > limit;

                if (hasMore)
                {
                    calls.RemoveAt(limit);
                }

                if (!fetchNext)
                {
                    calls.Reverse();
                }

                return (calls, hasMore, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated inbound calls for business {BusinessId}", businessId);
                return (new List<InboundCallQueueData>(), false, 0);
            }
        }

        public async Task UpdateInboundCallQueueSessionIdAndStatusAsync(string queueId, string sessionId, CallQueueStatusEnum status, DateTime completedAt)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.SessionId, sessionId)
                    .Set(c => c.Status, status)
                    .Set(c => c.CompletedAt, completedAt);

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

        public async Task SetInboundCallQueueFailedStatusAsync(string queueId, CallQueueLogEntry? log = null)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.Status, CallQueueStatusEnum.Failed);

                if (log != null)
                {
                    _ = _callQueueLogsRepository.AddCallLogAsync(queueId, log);
                }

                await _inboundCallQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating failed status for queue {QueueId}", queueId);
            }
        }

        public async Task UpdateInboundCallQueueProcessingBackendServerIdAsync(string queueId, string? serverId, CallQueueStatusEnum? status = null)
        {
            try
            {
                var filter = Builders<InboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<InboundCallQueueData>.Update
                    .Set(c => c.ProcessingBackendServerId, serverId);

                if (status != null)
                {
                    update = update.Set(c => c.Status, status);
                }

                await _inboundCallQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
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
                    Builders<InboundCallQueueData>.Filter.In(c => c.Status, new CallQueueStatusEnum[] { CallQueueStatusEnum.ProcessingProxy, CallQueueStatusEnum.ProcessedProxy, CallQueueStatusEnum.ProcessingBackend, CallQueueStatusEnum.ProcessedBackend }),
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

        public async Task<long?> GetInboundCallQueuesCountAsync(long businessId, GetBusinessInboundCallQueuesCountRequestModel modelData)
        {
            try
            {
                var filterBuilder = Builders<InboundCallQueueData>.Filter;
                var filterDefinitions = new List<FilterDefinition<InboundCallQueueData>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId),
                    filterBuilder.Eq(c => c.Type, CallQueueTypeEnum.Inbound)
                };

                // This filter-building logic is identical to the one in the pagination method for consistency
                if (modelData.StartCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, modelData.StartCreatedDate.Value.ToUniversalTime()));
                if (modelData.EndCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, modelData.EndCreatedDate.Value.ToUniversalTime()));
                if (modelData.StartCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CompletedAt, modelData.StartCompletedAtDate.Value.ToUniversalTime()));
                if (modelData.EndCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CompletedAt, modelData.EndCompletedAtDate.Value.ToUniversalTime()));
                if (modelData.QueueStatusTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, modelData.QueueStatusTypes));
                if (modelData.RouteIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteId, modelData.RouteIds));
                if (modelData.CallingNumbers?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallerNumber, modelData.CallingNumbers));
                if (modelData.RouteNumberProviders?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteNumberProvider, modelData.RouteNumberProviders));
                if (modelData.RouteNumberIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RouteNumberId, modelData.RouteNumberIds));

                var finalFilter = filterBuilder.And(filterDefinitions);

                return await _inboundCallQueueCollection.CountDocumentsAsync(finalFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inbound call queues count for business {BusinessId}", businessId);
                return null; // Return null to indicate failure to the manager
            }
        }
    }
}
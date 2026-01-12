using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.Queues.Outbound;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class OutboundCallQueueRepository
    {
        private readonly IMongoCollection<OutboundCallQueueData> _outboundQueueCollection;
        private readonly ILogger<OutboundCallQueueRepository> _logger;
        private readonly CallQueueLogsRepository _callQueueLogsRepository;

        private readonly string DatabaseName = "IqraCallQueue";
        private readonly string CollectionName = "OutboundCallQueue";

        public OutboundCallQueueRepository(IMongoClient client, ILogger<OutboundCallQueueRepository> logger, CallQueueLogsRepository callQueueLogsRepository)
        {
            _logger = logger;
            try
            {
                var database = client.GetDatabase(DatabaseName);
                _outboundQueueCollection = database.GetCollection<OutboundCallQueueData>(CollectionName);
                CreateIndexes();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error initializing OutboundCallQueueRepository: {ErrorMessage}", ex.Message);
                throw;
            }

            _callQueueLogsRepository = callQueueLogsRepository;
        }

        private void CreateIndexes()
        {
            var indexes = new[]
            {
                // Primary index for filtered pagination
                new CreateIndexModel<OutboundCallQueueData>(
                    Builders<OutboundCallQueueData>.IndexKeys
                        .Ascending(c => c.BusinessId)
                        .Ascending(c => c.Status)
                        .Descending(c => c.CreatedAt)
                        .Descending(c => c.Id),
                    new CreateIndexOptions<OutboundCallQueueData> { Name = "Idx_Business_Status_CreatedAt_Id" }), // Using generic for consistency

                // For GetProcessableOutboundCallsAndMarkAsync
                new CreateIndexModel<OutboundCallQueueData>(
                    Builders<OutboundCallQueueData>.IndexKeys
                        .Ascending(c => c.RegionId)
                        .Ascending(c => c.Status)
                        .Ascending(c => c.ScheduledForDateTime),
                    new CreateIndexOptions<OutboundCallQueueData> { Name = "Idx_Region_Status_ScheduledFor" }), // Using generic for consistency

                // For finding calls being processed by a specific server
                new CreateIndexModel<OutboundCallQueueData>(
                    Builders<OutboundCallQueueData>.IndexKeys
                        .Ascending(c => c.ProcessingBackendServerId)
                        .Ascending(c => c.Status),
                    new CreateIndexOptions<OutboundCallQueueData> { Name = "Idx_ProcessingServerId_Status" }), // Using generic for consistency

                // For general campaign and status lookups
                new CreateIndexModel<OutboundCallQueueData>(
                    Builders<OutboundCallQueueData>.IndexKeys
                        .Ascending(c => c.CampaignId)
                        .Ascending(c => c.Status),
                    new CreateIndexOptions<OutboundCallQueueData> { Name = "Idx_CampaignId_Status" }), // Using generic for consistency

                // For looking up calls by SessionId with a partial filter
                new CreateIndexModel<OutboundCallQueueData>(
                    Builders<OutboundCallQueueData>.IndexKeys.Ascending(c => c.SessionId),
                    new CreateIndexOptions<OutboundCallQueueData>
                    {
                        Name = "Idx_SessionId",
                        Unique = true,
                        PartialFilterExpression = Builders<OutboundCallQueueData>.Filter.Type(c => c.SessionId, "string")
                    })
            };

            _outboundQueueCollection.Indexes.CreateManyAsync(indexes).GetAwaiter().GetResult();
        }

        public async Task<string?> EnqueueOutboundCallAsync(OutboundCallQueueData callQueueData)
        {
            try
            {
                await _outboundQueueCollection.InsertOneAsync(callQueueData);
                return callQueueData.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing outbound call for BusinessId {BusinessId}, QueueId {QueueId}", callQueueData.BusinessId, callQueueData.Id);
                return null;
            }
        }

        public async Task<(List<OutboundCallQueueData> Items, bool HasMore, long TotalCount)> GetOutboundCallQueuesForBusinessPaginatedAsync(
            long businessId,
            GetBusinessOutboundCallQueuesRequestFilterModel filter, // MODIFIED: Accepts the specific filter model
            int limit,                                             // MODIFIED: Accepts the limit directly
            PaginationCursor<GetBusinessOutboundCallQueuesRequestFilterModel>? cursor, // MODIFIED: Accepts the generic cursor
            bool fetchNext)
        {
            try
            {
                var filterBuilder = Builders<OutboundCallQueueData>.Filter;

                var filterDefinitions = new List<FilterDefinition<OutboundCallQueueData>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId)
                };

                // --- MODIFIED: Build the query directly from the 'filter' parameter ---
                if (filter.StartCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, filter.StartCreatedDate.Value.ToUniversalTime()));
                if (filter.EndCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, filter.EndCreatedDate.Value.ToUniversalTime()));
                if (filter.StartCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CompletedAt, filter.StartCompletedAtDate.Value.ToUniversalTime()));
                if (filter.EndCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CompletedAt, filter.EndCompletedAtDate.Value.ToUniversalTime()));
                if (filter.StartScheduledDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.ScheduledForDateTime, filter.StartScheduledDate.Value.ToUniversalTime()));
                if (filter.EndScheduledDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.ScheduledForDateTime, filter.EndScheduledDate.Value.ToUniversalTime()));
                if (filter.QueueStatusTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, filter.QueueStatusTypes));
                if (filter.CampaignIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CampaignId, filter.CampaignIds));
                if (filter.CallingNumberIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallingNumberId, filter.CallingNumberIds));
                if (filter.CallingNumberProviders?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallingNumberProvider, filter.CallingNumberProviders));
                if (filter.RecipientNumbers?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RecipientNumber, filter.RecipientNumbers));

                var baseFilter = filterBuilder.And(filterDefinitions);

                long totalCount = await _outboundQueueCollection.CountDocumentsAsync(baseFilter);

                FilterDefinition<OutboundCallQueueData> finalFilter = baseFilter;
                SortDefinition<OutboundCallQueueData> sortDefinition;

                if (fetchNext)
                {
                    sortDefinition = Builders<OutboundCallQueueData>.Sort
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
                    sortDefinition = Builders<OutboundCallQueueData>.Sort
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
                        return (new List<OutboundCallQueueData>(), false, 0);
                    }
                }

                // Fetch one extra item to determine if a next/previous page exists
                var items = await _outboundQueueCollection.Find(finalFilter)
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
                _logger.LogError(ex, "Error getting paginated outbound calls for business {BusinessId}", businessId);
                return (new List<OutboundCallQueueData>(), false, 0);
            }
        }

        public async Task<(List<OutboundCallQueueData> processedCalls, PaginationCursor<PaginationCursorNoFilterHelper>? nextCursor)> GetProcessableOutboundCallsAndMarkAsync(
            string regionId,
            int batchSizeToFetch,
            DateTime scheduleThreshold,
            PaginationCursor<PaginationCursorNoFilterHelper>? previousRequestLastSeenCursor)
        {
            var successfullyMarkedCalls = new List<OutboundCallQueueData>();
            PaginationCursor<PaginationCursorNoFilterHelper>? newLastSeenCursorInThisBatch = null;

            try
            {
                var filterBuilder = Builders<OutboundCallQueueData>.Filter;
                var baseFilter = filterBuilder.And(
                    filterBuilder.Eq(c => c.RegionId, regionId),
                    filterBuilder.Eq(c => c.Status, CallQueueStatusEnum.Queued),
                    filterBuilder.Lte(c => c.ScheduledForDateTime, scheduleThreshold)
                );

                FilterDefinition<OutboundCallQueueData> finalFilter = baseFilter;
                var sort = Builders<OutboundCallQueueData>.Sort
                    .Ascending(c => c.ScheduledForDateTime)
                    .Ascending(c => c.Id);

                if (previousRequestLastSeenCursor != null)
                {
                    var cursorFilter = filterBuilder.Or(
                        filterBuilder.Gt(c => c.ScheduledForDateTime, previousRequestLastSeenCursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(c => c.ScheduledForDateTime, previousRequestLastSeenCursor.Timestamp),
                            filterBuilder.Gt(c => c.Id, previousRequestLastSeenCursor.Id)
                        )
                    );
                    finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                }

                var candidateCalls = await _outboundQueueCollection.Find(finalFilter)
                    .Sort(sort)
                    .Limit(batchSizeToFetch)
                    .ToListAsync();

                if (!candidateCalls.Any())
                {
                    return (successfullyMarkedCalls, null);
                }

                var now = DateTime.UtcNow;
                foreach (var call in candidateCalls)
                {
                    newLastSeenCursorInThisBatch = new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = call.ScheduledForDateTime, Id = call.Id };

                    var updateFilter = Builders<OutboundCallQueueData>.Filter.And(
                        filterBuilder.Eq(c => c.Id, call.Id),
                        filterBuilder.Eq(c => c.Status, CallQueueStatusEnum.Queued)
                    );

                    var updateDefinition = Builders<OutboundCallQueueData>.Update
                        .Set(c => c.Status, CallQueueStatusEnum.ProcessingProxy)
                        .Set(c => c.EnqueuedAt, call.EnqueuedAt ?? now);

                    var options = new FindOneAndUpdateOptions<OutboundCallQueueData>
                    {
                        ReturnDocument = ReturnDocument.After
                    };

                    try
                    {
                        var updatedCall = await _outboundQueueCollection.FindOneAndUpdateAsync(updateFilter, updateDefinition, options);
                        if (updatedCall != null)
                        {
                            successfullyMarkedCalls.Add(updatedCall);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error marking call {CallId} for processing during paginated fetch.", call.Id);
                    }
                }

                if (candidateCalls.Count < batchSizeToFetch && successfullyMarkedCalls.Count == 0 && candidateCalls.Any())
                {
                    return (successfullyMarkedCalls, null);
                }

                return (successfullyMarkedCalls, newLastSeenCursorInThisBatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProcessableOutboundCallsAndMarkAsync for region {RegionId}", regionId);
                return (new List<OutboundCallQueueData>(), previousRequestLastSeenCursor);
            }
        }

        public async Task<bool> UnmarkProcessableOutboundCallsAsync(List<string> queueIds)
        {
            try
            {
                var updateDefinition = Builders<OutboundCallQueueData>.Update
                        .Set(c => c.Status, CallQueueStatusEnum.Queued)
                        .Set(c => c.EnqueuedAt, null);

                var filter = Builders<OutboundCallQueueData>.Filter.In(c => c.Id, queueIds);

                var result = await _outboundQueueCollection.UpdateManyAsync(filter, updateDefinition);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UnmarkProcessableOutboundCallsAsync");
                return false;
            }
        }

        public async Task<bool> UpdateCallStatusAsync(
            string queueId, CallQueueStatusEnum newStatus,
            CallQueueLogEntry? log = null,
            string? newProcessingServerId = null,
            DateTime? processingStartedAt = null,
            DateTime? completedAt = null,
            Dictionary<string, string>? providerMetadata = null,
            string? providerCallId = null)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var updateBuilder = Builders<OutboundCallQueueData>.Update.Set(c => c.Status, newStatus);

                if (log != null)
                {
                    _ = _callQueueLogsRepository.AddCallLogAsync(queueId, log);
                }

                if (newProcessingServerId != null)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingBackendServerId, newProcessingServerId);
                }
                else if (newStatus == CallQueueStatusEnum.Queued)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingBackendServerId, (string?)null);
                }

                if (processingStartedAt.HasValue)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingStartedAt, processingStartedAt.Value);
                }
                else if (newStatus == CallQueueStatusEnum.ProcessingProxy && !processingStartedAt.HasValue)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingStartedAt, DateTime.UtcNow);
                }

                if (completedAt.HasValue)
                {
                    updateBuilder = updateBuilder.Set(c => c.CompletedAt, completedAt.Value);
                }
                else if (newStatus == CallQueueStatusEnum.ProcessedBackend || newStatus == CallQueueStatusEnum.Failed ||
                         newStatus == CallQueueStatusEnum.Canceled || newStatus == CallQueueStatusEnum.Expired)
                {
                    updateBuilder = updateBuilder.Set(c => c.CompletedAt, DateTime.UtcNow);
                }

                if (providerMetadata != null)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProviderMetadata, providerMetadata);
                }
                if (providerCallId != null)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProviderCallId, providerCallId);
                }

                var result = await _outboundQueueCollection.UpdateOneAsync(filter, updateBuilder);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for outbound call {QueueId} to {NewStatus}", queueId, newStatus);
                return false;
            }
        }

        public async Task<bool> AddCallLogAsync(string queueId, CallQueueLogEntry log)
        {
            return await _callQueueLogsRepository.AddCallLogAsync(queueId, log);
        }

        public async Task<OutboundCallQueueData?> GetOutboundCallQueueByIdAsync(string queueId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                return await _outboundQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call by ID {QueueId}", queueId);
                return null;
            }
        }

        public async Task<OutboundCallQueueData?> GetOutboundCallQueueByIdAsync(long businessId, string queueId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.And(
                    Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId),
                    Builders<OutboundCallQueueData>.Filter.Eq(c => c.BusinessId, businessId)
                );

                return await _outboundQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call by ID {QueueId}", queueId);
                return null;
            }
        }

        public async Task<OutboundCallQueueData?> GetOutboundCallQueueBySessionIdAsync(string sessionId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.SessionId, sessionId);
                return await _outboundQueueCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call by Session ID {SessionId}", sessionId);
                return null;
            }
        }

        public async Task<List<OutboundCallQueueData>> GetOutboundCallsByCampaignIdAsync(string campaignId)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.CampaignId, campaignId);
                return await _outboundQueueCollection.Find(filter).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound calls for CampaignId {CampaignId}", campaignId);
                return new List<OutboundCallQueueData>();
            }
        }

        public async Task UpdateOutboundCallQueueSessionIdAndStatusAsync(string queueId, string sessionId, CallQueueStatusEnum status, DateTime completedAt)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var update = Builders<OutboundCallQueueData>.Update
                    .Set(c => c.SessionId, sessionId)
                    .Set(c => c.Status, status)
                    .Set(c => c.CompletedAt, completedAt);

                await _outboundQueueCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session ID and status for queue {QueueId}", queueId);
            }
        }

        public async Task<long?> GetOutboundCallQueuesCountAsync(long businessId, GetBusinessOutboundCallQueuesCountRequestModel modelData)
        {
            try
            {
                var filterBuilder = Builders<OutboundCallQueueData>.Filter;
                var filterDefinitions = new List<FilterDefinition<OutboundCallQueueData>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId)
                };

                if (modelData.StartCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, modelData.StartCreatedDate.Value));
                if (modelData.EndCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, modelData.EndCreatedDate.Value));
                if (modelData.StartCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CompletedAt, modelData.StartCompletedAtDate.Value));
                if (modelData.EndCompletedAtDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CompletedAt, modelData.EndCompletedAtDate.Value));
                if (modelData.StartScheduledDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.ScheduledForDateTime, modelData.StartScheduledDate.Value));
                if (modelData.EndScheduledDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.ScheduledForDateTime, modelData.EndScheduledDate.Value));
                if (modelData.QueueStatusTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, modelData.QueueStatusTypes));
                if (modelData.CampaignIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CampaignId, modelData.CampaignIds));
                if (modelData.CallingNumberIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallingNumberId, modelData.CallingNumberIds));
                if (modelData.CallingNumberProviders?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.CallingNumberProvider, modelData.CallingNumberProviders));
                if (modelData.RecipientNumbers?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.RecipientNumber, modelData.RecipientNumbers));

                var finalFilter = filterBuilder.And(filterDefinitions);
                return await _outboundQueueCollection.CountDocumentsAsync(finalFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call queues count for business {BusinessId}", businessId);
                return null;
            }
        }
    
        public async Task<bool> CancelBusinessCallQueuesAsync(long businessId, IClientSessionHandle session)
        {
            var filter = Builders<OutboundCallQueueData>.Filter.And(
                Builders<OutboundCallQueueData>.Filter.Eq(c => c.BusinessId, businessId),
                Builders<OutboundCallQueueData>.Filter.Eq(c => c.Status, CallQueueStatusEnum.Queued)
            );

            var update = Builders<OutboundCallQueueData>.Update.Set(c => c.Status, CallQueueStatusEnum.Canceled);
            var result = await _outboundQueueCollection.UpdateManyAsync(session, filter, update);

            return result.IsAcknowledged;
        }
    }
}
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Call
{
    public class OutboundCallQueueRepository
    {
        private readonly IMongoCollection<OutboundCallQueueData> _outboundActiveQueueCollection;
        private readonly IMongoCollection<OutboundCallQueueData> _outboundArchivedQueueCollection;
        private readonly ILogger<OutboundCallQueueRepository> _logger;

        private const string ActiveCollectionName = "OutboundCallQueue_Active";
        private const string ArchivedCollectionName = "OutboundCallQueue_Archived";

        public OutboundCallQueueRepository(string connectionString, string databaseName, ILogger<OutboundCallQueueRepository> logger)
        {
            _logger = logger;
            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _outboundActiveQueueCollection = database.GetCollection<OutboundCallQueueData>(ActiveCollectionName);
                _outboundArchivedQueueCollection = database.GetCollection<OutboundCallQueueData>(ArchivedCollectionName);

                CreateIndexes();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error initializing OutboundCallQueueRepository: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private void CreateIndexes()
        {
            var activeIndexes = new[]
            {
                    // For GetProcessableOutboundCallsAndMarkAsync
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys
                            .Ascending(c => c.RegionId)
                            .Ascending(c => c.Status)
                            .Ascending(c => c.ScheduledForDateTime),
                        new CreateIndexOptions { Name = "Idx_Active_Region_Status_ScheduledFor" }),

                    // For general lookups by BusinessId and Status
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys
                            .Ascending(c => c.BusinessId)
                            .Ascending(c => c.Status),
                        new CreateIndexOptions { Name = "Idx_Active_BusinessId_Status" }),

                    // For finding calls being processed by a specific proxy instance
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys
                            .Ascending(c => c.ProcessingServerId) // Assuming this will store Proxy Instance ID
                            .Ascending(c => c.Status),
                        new CreateIndexOptions { Name = "Idx_Active_ProcessingServerId_Status" }),

                     // For campaign related queries on active calls
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys.Ascending(c => c.CampaignId),
                        new CreateIndexOptions { Name = "Idx_Active_CampaignId" }),
                };
            _outboundActiveQueueCollection.Indexes.CreateManyAsync(activeIndexes).GetAwaiter().GetResult();

            // Indexes for OutboundCallQueue_Archived
            var archivedIndexes = new[]
            {
                    // For fetching archived calls by BusinessId, sorted by completion
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys
                            .Ascending(c => c.BusinessId)
                            .Descending(c => c.CompletedAt), // Often you query recent completed calls
                        new CreateIndexOptions { Name = "Idx_Archived_BusinessId_CompletedAt" }),

                    // For fetching archived calls by CampaignId
                    new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys.Ascending(c => c.CampaignId),
                        new CreateIndexOptions { Name = "Idx_Archived_CampaignId" }),

                    // For general lookup by ID
                     new CreateIndexModel<OutboundCallQueueData>(
                        Builders<OutboundCallQueueData>.IndexKeys.Ascending(c => c.Id),
                        new CreateIndexOptions { Name = "Idx_Archived_Id", Unique = true }),
                };
            _outboundArchivedQueueCollection.Indexes.CreateManyAsync(archivedIndexes).GetAwaiter().GetResult();
        }

        public async Task<string?> EnqueueOutboundCallAsync(OutboundCallQueueData callQueueData)
        {
            try
            {
                await _outboundActiveQueueCollection.InsertOneAsync(callQueueData);
                return callQueueData.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing outbound call for BusinessId {BusinessId}, QueueId {QueueId}", callQueueData.BusinessId, callQueueData.Id);
                return null;
            }
        }

        public async Task<(List<OutboundCallQueueData> processedCalls, PaginationCursor? nextCursor)> GetProcessableOutboundCallsAndMarkAsync(
            string regionId,
            int batchSizeToFetch, // How many to consider for processing in this round
            int maxToProcess,     // Max calls this proxy instance wants to actually process now
            DateTime scheduleThreshold,
            string proxyInstanceId,
            PaginationCursor? previousRequestLastSeenCursor
        ) // Cursor from the previous call to this method
        {
            var successfullyMarkedCalls = new List<OutboundCallQueueData>();
            if (batchSizeToFetch <= 0 || maxToProcess <= 0)
            {
                return (successfullyMarkedCalls, previousRequestLastSeenCursor);
            }

            PaginationCursor? newLastSeenCursorInThisBatch = null;

            try
            {
                var filterBuilder = Builders<OutboundCallQueueData>.Filter;
                var baseFilter = filterBuilder.And(
                    filterBuilder.Eq(c => c.RegionId, regionId),
                    filterBuilder.Eq(c => c.Status, CallQueueStatusEnum.Queued),
                    filterBuilder.Lte(c => c.ScheduledForDateTime, scheduleThreshold)
                );

                FilterDefinition<OutboundCallQueueData> finalFilter = baseFilter;
                // Sort by oldest scheduled first, then by ID for stable ordering
                var sort = Builders<OutboundCallQueueData>.Sort
                    .Ascending(c => c.ScheduledForDateTime)
                    .Ascending(c => c.Id);

                if (previousRequestLastSeenCursor != null)
                {
                    // Apply cursor filter: get items *after* the previous cursor
                    var cursorFilter = filterBuilder.Or(
                        filterBuilder.Gt(c => c.ScheduledForDateTime, previousRequestLastSeenCursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(c => c.ScheduledForDateTime, previousRequestLastSeenCursor.Timestamp),
                            filterBuilder.Gt(c => c.Id, previousRequestLastSeenCursor.Id)
                        )
                    );
                    finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                }

                // Fetch a batch of candidates.
                var candidateCalls = await _outboundActiveQueueCollection.Find(finalFilter)
                                                                    .Sort(sort)
                                                                    .Limit(batchSizeToFetch)
                                                                    .ToListAsync();

                if (!candidateCalls.Any())
                {
                    // No more calls after the cursor, or no calls at all if cursor was null.
                    // If cursor was null and no calls, means queue is empty for criteria.
                    // If cursor was not null, means we reached the end of the queue for criteria.
                    // The caller (service) might decide to reset its cursor to null to start from beginning next time.
                    return (successfullyMarkedCalls, null); // Signal to reset cursor
                }

                var now = DateTime.UtcNow;
                foreach (var call in candidateCalls)
                {
                    newLastSeenCursorInThisBatch = new PaginationCursor { Timestamp = call.ScheduledForDateTime, Id = call.Id };

                    if (successfullyMarkedCalls.Count >= maxToProcess)
                    {
                        // We have enough calls for this processing cycle
                        break;
                    }

                    var updateFilter = Builders<OutboundCallQueueData>.Filter.And(
                        filterBuilder.Eq(c => c.Id, call.Id),
                        filterBuilder.Eq(c => c.Status, CallQueueStatusEnum.Queued) // Ensure it's still Queued (important!)
                    );

                    var updateDefinition = Builders<OutboundCallQueueData>.Update
                        .Set(c => c.Status, CallQueueStatusEnum.WaitingForProcessing)
                        .Set(c => c.ProcessingServerId, proxyInstanceId)
                        .Set(c => c.EnqueuedAt, call.EnqueuedAt ?? now) // Ensure EnqueuedAt is set
                        .Push(c => c.Logs, new CallQueueLog { CreatedAt = now, Type = CallQueueLogTypeEnum.System, Message = $"Picked up by proxy {proxyInstanceId}" });

                    var options = new FindOneAndUpdateOptions<OutboundCallQueueData>
                    {
                        ReturnDocument = ReturnDocument.After
                    };

                    try
                    {
                        var updatedCall = await _outboundActiveQueueCollection.FindOneAndUpdateAsync(updateFilter, updateDefinition, options);
                        if (updatedCall != null)
                        {
                            successfullyMarkedCalls.Add(updatedCall);
                        }
                        // If updatedCall is null, another proxy instance or process grabbed it, or its status changed.
                        // This is fine, we just move to the next candidate.
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error marking call {CallId} for processing by proxy {ProxyId} during paginated fetch.", call.Id, proxyInstanceId);
                    }
                }

                // If we fetched fewer items than batchSizeToFetch, it means we might have reached the end of the current queue snapshot.
                // The newLastSeenCursorInThisBatch will be the last item considered.
                // If candidateCalls.Count < batchSizeToFetch, the service might want to reset its cursor for the next full poll.
                // Or, if newLastSeenCursorInThisBatch is the same as previousRequestLastSeenCursor and no calls were processed, it might indicate no progress.
                if (candidateCalls.Count < batchSizeToFetch && successfullyMarkedCalls.Count == 0 && candidateCalls.Any())
                {
                    // We scanned a partial batch, found nothing we could process, and it was the end of the list.
                    // Signal to service to reset its internal cursor to null for next full scan.
                    return (successfullyMarkedCalls, null);
                }


                return (successfullyMarkedCalls, newLastSeenCursorInThisBatch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetProcessableOutboundCallsAndMarkAsync for region {RegionId} by proxy {ProxyId}", regionId, proxyInstanceId);
                return (new List<OutboundCallQueueData>(), previousRequestLastSeenCursor); // Return old cursor on error to retry same range
            }
        }

        public async Task<bool> UpdateCallStatusAsync(
            string queueId, CallQueueStatusEnum newStatus,
            CallQueueLog? log = null,
            string? newProcessingServerId = null, // For when backend takes over
            DateTime? processingStartedAt = null,
            DateTime? completedAt = null,
            Dictionary<string, string>? providerMetadata = null,
            string? providerCallId = null
        )
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var updateBuilder = Builders<OutboundCallQueueData>.Update.Set(c => c.Status, newStatus);

                if (log != null)
                {
                    log.CreatedAt = DateTime.UtcNow;
                    updateBuilder = updateBuilder.Push(c => c.Logs, log);
                }

                if (newProcessingServerId != null) // Can be null to unset, or a new server ID
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingServerId, newProcessingServerId);
                }
                else if (newStatus == CallQueueStatusEnum.Queued) // If requeueing, clear ProcessingServerId
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingServerId, (string?)null);
                }


                if (processingStartedAt.HasValue)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingStartedAt, processingStartedAt.Value);
                }
                else if (newStatus == CallQueueStatusEnum.Processing && !processingStartedAt.HasValue) // Set if transitioning to Processing
                {
                    updateBuilder = updateBuilder.Set(c => c.ProcessingStartedAt, DateTime.UtcNow);
                }


                if (completedAt.HasValue)
                {
                    updateBuilder = updateBuilder.Set(c => c.CompletedAt, completedAt.Value);
                }
                else if (newStatus == CallQueueStatusEnum.Processed || newStatus == CallQueueStatusEnum.Failed ||
                         newStatus == CallQueueStatusEnum.Canceled || newStatus == CallQueueStatusEnum.Expired)
                {
                    updateBuilder = updateBuilder.Set(c => c.CompletedAt, DateTime.UtcNow);
                }

                if (providerMetadata != null)
                {
                    // This will overwrite the existing dictionary. If you need to merge, fetch first or use $set for specific keys.
                    updateBuilder = updateBuilder.Set(c => c.ProviderMetadata, providerMetadata);
                }
                if (providerCallId != null)
                {
                    updateBuilder = updateBuilder.Set(c => c.ProviderCallId, providerCallId);
                }

                var result = await _outboundActiveQueueCollection.UpdateOneAsync(filter, updateBuilder);
                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for outbound call {QueueId} to {NewStatus}", queueId, newStatus);
                return false;
            }
        }

        public async Task<bool> MoveToArchivedAsync(string queueId, CallQueueStatusEnum finalStatus, CallQueueLog? finalLog = null)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var callToArchive = await _outboundActiveQueueCollection.Find(filter).FirstOrDefaultAsync();

                if (callToArchive == null)
                {
                    var alreadyArchived = await _outboundArchivedQueueCollection.Find(filter).FirstOrDefaultAsync();
                    return alreadyArchived != null; // Return true if already archived
                }

                // Update final properties before archiving
                callToArchive.Status = finalStatus;
                callToArchive.CompletedAt = DateTime.UtcNow;
                if (finalLog != null)
                {
                    callToArchive.Logs.Add(finalLog);
                }

                await _outboundArchivedQueueCollection.InsertOneAsync(callToArchive);
                var deleteResult = await _outboundActiveQueueCollection.DeleteOneAsync(filter);

                if (deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving call {QueueId} to archive with status {FinalStatus}", queueId, finalStatus);
                return false;
            }
        }

        public async Task<OutboundCallQueueData?> GetOutboundCallQueueByIdAsync(string queueId, bool searchArchivedIfNotFoundInActive = true)
        {
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.Id, queueId);
                var call = await _outboundActiveQueueCollection.Find(filter).FirstOrDefaultAsync();

                if (call == null && searchArchivedIfNotFoundInActive)
                {
                    call = await _outboundArchivedQueueCollection.Find(filter).FirstOrDefaultAsync();
                }
                return call;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound call by ID {QueueId}", queueId);
                return null;
            }
        }

        public async Task<List<OutboundCallQueueData>> GetOutboundCallsByCampaignIdAsync(string campaignId, bool searchArchived = false)
        {
            var calls = new List<OutboundCallQueueData>();
            try
            {
                var filter = Builders<OutboundCallQueueData>.Filter.Eq(c => c.CampaignId, campaignId);
                calls.AddRange(await _outboundActiveQueueCollection.Find(filter).ToListAsync());

                if (searchArchived)
                {
                    calls.AddRange(await _outboundArchivedQueueCollection.Find(filter).ToListAsync());
                }
                return calls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting outbound calls for CampaignId {CampaignId}", campaignId);
                return calls; // return whatever was fetched so far or an empty list
            }
        }

        
    }
}

using IqraCore.Entities.Helpers;
using IqraCore.Entities.WebSession;
using IqraCore.Models.Business.WebSession;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.WebSession
{
    public class WebSessionRepository
    {
        private readonly IMongoCollection<WebSessionData> _webSessionCollection;
        private readonly ILogger<WebSessionRepository> _logger;

        private const string WebSessionCollectionName = "WebSession";

        public WebSessionRepository(ILogger<WebSessionRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            var database = client.GetDatabase(databaseName);
            _webSessionCollection = database.GetCollection<WebSessionData>(WebSessionCollectionName);

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexes = new[]
            {
                new CreateIndexModel<WebSessionData>(
                    Builders<WebSessionData>.IndexKeys
                        .Ascending(c => c.BusinessId)
                        .Descending(c => c.CreatedAt)
                        .Descending(c => c.Id),
                    new CreateIndexOptions { Name = "Idx_WebSession_Business_CreatedAt_Id" })
            };

            try
            {
                _webSessionCollection.Indexes.CreateManyAsync(indexes).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for WebSession collection");
            }
        }

        public async Task<(List<WebSessionData> Items, bool HasMore, long TotalCount)> GetWebSessionsForBusinessPaginatedAsync(
            long businessId,
            GetBusinessWebSessionsRequestFilterModel filter,
            int limit,
            PaginationCursor<GetBusinessWebSessionsRequestFilterModel>? cursor,
            bool fetchNext)
        {
            try
            {
                var filterBuilder = Builders<WebSessionData>.Filter;
                var filterDefinitions = new List<FilterDefinition<WebSessionData>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId)
                };

                // --- Dynamic Filtering ---
                if (filter.StartCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, filter.StartCreatedDate.Value.ToUniversalTime()));
                if (filter.EndCreatedDate.HasValue)
                    filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, filter.EndCreatedDate.Value.ToUniversalTime()));

                // Note: WebSessionData currently does not have CompletedAt, so we skip StartCompletedAtDate/EndCompletedAtDate

                if (filter.QueueStatusTypes?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.Status, filter.QueueStatusTypes));

                if (filter.WebCampaignIds?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.WebCampaignId, filter.WebCampaignIds));

                if (filter.ClientIdentifiers?.Any() == true)
                    filterDefinitions.Add(filterBuilder.In(c => c.ClientIdentifier, filter.ClientIdentifiers));

                var baseFilter = filterBuilder.And(filterDefinitions);

                // --- Total Count (Pre-Pagination) ---
                long totalCount = await _webSessionCollection.CountDocumentsAsync(baseFilter);

                // --- Cursor & Sorting Logic ---
                FilterDefinition<WebSessionData> finalFilter = baseFilter;
                SortDefinition<WebSessionData> sortDefinition;

                if (fetchNext)
                {
                    // Default View: Newest First
                    sortDefinition = Builders<WebSessionData>.Sort
                        .Descending(c => c.CreatedAt)
                        .Descending(c => c.Id);

                    if (cursor != null)
                    {
                        // Get items older than the cursor
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Lt(c => c.CreatedAt, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.CreatedAt, cursor.Timestamp), filterBuilder.Lt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                }
                else // Fetching Previous Page
                {
                    // Temporary Sort: Oldest First (to get the "previous" 12 items)
                    sortDefinition = Builders<WebSessionData>.Sort
                        .Ascending(c => c.CreatedAt)
                        .Ascending(c => c.Id);

                    if (cursor != null)
                    {
                        // Get items newer than the cursor
                        var cursorFilter = filterBuilder.Or(
                            filterBuilder.Gt(c => c.CreatedAt, cursor.Timestamp),
                            filterBuilder.And(filterBuilder.Eq(c => c.CreatedAt, cursor.Timestamp), filterBuilder.Gt(c => c.Id, cursor.Id))
                        );
                        finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                    }
                    else
                    {
                        // Edge case: Requesting previous but no cursor? Return empty.
                        return (new List<WebSessionData>(), false, 0);
                    }
                }

                // --- Execution ---
                var items = await _webSessionCollection.Find(finalFilter)
                    .Sort(sortDefinition)
                    .Limit(limit + 1) // Fetch one extra to check HasMore
                    .ToListAsync();

                bool hasMore = items.Count > limit;

                if (hasMore)
                {
                    items.RemoveAt(limit); // Remove the extra item
                }

                // If we fetched "Previous" (Ascending), flip back to Descending for UI
                if (!fetchNext)
                {
                    items.Reverse();
                }

                return (items, hasMore, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated web sessions for business {BusinessId}", businessId);
                return (new List<WebSessionData>(), false, 0);
            }
        }

        public async Task<long?> GetWebSessionsCountAsync(long businessId, GetBusinessWebSessionsRequestModel modelData)
        {
            try
            {
                var filterBuilder = Builders<WebSessionData>.Filter;
                var filterDefinitions = new List<FilterDefinition<WebSessionData>>
                {
                    filterBuilder.Eq(c => c.BusinessId, businessId)
                };

                // Replicate filtering logic exactly as above
                if (modelData.Filter != null)
                {
                    var filter = modelData.Filter;
                    if (filter.StartCreatedDate.HasValue)
                        filterDefinitions.Add(filterBuilder.Gte(c => c.CreatedAt, filter.StartCreatedDate.Value.ToUniversalTime()));
                    if (filter.EndCreatedDate.HasValue)
                        filterDefinitions.Add(filterBuilder.Lte(c => c.CreatedAt, filter.EndCreatedDate.Value.ToUniversalTime()));
                    if (filter.QueueStatusTypes?.Any() == true)
                        filterDefinitions.Add(filterBuilder.In(c => c.Status, filter.QueueStatusTypes));
                    if (filter.WebCampaignIds?.Any() == true)
                        filterDefinitions.Add(filterBuilder.In(c => c.WebCampaignId, filter.WebCampaignIds));
                    if (filter.ClientIdentifiers?.Any() == true)
                        filterDefinitions.Add(filterBuilder.In(c => c.ClientIdentifier, filter.ClientIdentifiers));
                }

                var finalFilter = filterBuilder.And(filterDefinitions);

                return await _webSessionCollection.CountDocumentsAsync(finalFilter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting web session count for business {BusinessId}", businessId);
                return null;
            }
        }

        public async Task<bool> AddWebSessionAsync(WebSessionData newWebSessionData)
        {
            try
            {
                await _webSessionCollection.InsertOneAsync(newWebSessionData);

                return true;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error in AddWebSessionAsync");
                return false;
            }
        }

        public async Task<WebSessionData?> GetWebSessionByIdAsync(string webSessionId)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);

                return await _webSessionCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetWebSessionByIdAsync");
                return null;
            }
        }

        public async Task<bool> UpdateStatusAndAddLogAsync(string id, WebSessionStatusEnum failed, WebSessionLog webSessionLog)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, id);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, failed)
                    .Push(x => x.Logs, webSessionLog);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusAndAddLogAsync");
                return false;
            }
        }

        public async Task<bool> UpdateStatusProcessedBackendWithServerIdAndWebsocketURL(string webSessionId, string sessionId, string websocketUrl)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, WebSessionStatusEnum.ProcessedBackend)
                    .Set(x => x.SessionId, sessionId)
                    .Set(x => x.SessionWebSocketUrl, websocketUrl);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusProcessedBackendWithServerIdAndWebsocketURL");
                return false;
            }
        }

        public async Task<bool> UpdateStatusProcessingBackendWithServerId(string webSessionId, string serverId)
        {
            try
            {
                var filter = Builders<WebSessionData>.Filter.Eq(x => x.Id, webSessionId);
                var update = Builders<WebSessionData>.Update
                    .Set(x => x.Status, WebSessionStatusEnum.ProcessingBackend)
                    .Set(x => x.SessionRegionBackendServerId, serverId);

                var result = await _webSessionCollection.UpdateOneAsync(filter, update);

                return result.IsAcknowledged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateStatusProcessingBackendWithServerId");
                return false;
            }
        }
    }
}

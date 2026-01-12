using IqraCore.Entities.Helpers;
using IqraCore.Entities.Usage;
using IqraCore.Entities.User.Usage;
using IqraCore.Entities.User.Usage.Enums;
using IqraCore.Models.Usage;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.User
{
    public class UserUsageRepository
    {
        private readonly ILogger<UserUsageRepository> _logger;
        private readonly IMongoCollection<UserUsageRecordData> _userUsageCollection;

        private readonly string DatabaseName = "IqraWebSession";
        private readonly string CollectionName = "UserUsageRecords";

        public UserUsageRepository(ILogger<UserUsageRepository> logger, IMongoClient client)
        {
            _logger = logger;

            var database = client.GetDatabase(DatabaseName);
            _userUsageCollection = database.GetCollection<UserUsageRecordData>(CollectionName);

            var indexKeysDefinition = Builders<UserUsageRecordData>.IndexKeys
                .Ascending(r => r.BusinessId)
                .Ascending(r => r.CreatedAt);
            _userUsageCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserUsageRecordData>(indexKeysDefinition));
        }

        // Basic CRUD Operations
        public Task AddUserUsageRecordAsync(UserUsageRecordData record)
        {
            return _userUsageCollection.InsertOneAsync(record);
        }

        public async Task<bool> AddUserUsageRecordAsync(UserUsageRecordData record, IClientSessionHandle session)
        {
            try
            {
                await _userUsageCollection.InsertOneAsync(session, record);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // Read Operations
        public Task<List<UserUsageRecordData>> GetUserUsageHistoryAsync(string userEmail, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<UserUsageRecordData>.Filter.And(
                Builders<UserUsageRecordData>.Filter.Eq(r => r.BusinessMasterUserEmail, userEmail),
                Builders<UserUsageRecordData>.Filter.Gte(r => r.CreatedAt, startDate),
                Builders<UserUsageRecordData>.Filter.Lt(r => r.CreatedAt, endDate)
            );

            return _userUsageCollection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync();
        }

        public Task<List<UserUsageRecordData>> GetUserBusinessUsageHistoryAsync(string userEmail, long businessId, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<UserUsageRecordData>.Filter.And(
                Builders<UserUsageRecordData>.Filter.Eq(r => r.BusinessMasterUserEmail, userEmail),
                Builders<UserUsageRecordData>.Filter.Eq(r => r.BusinessId, businessId),
                Builders<UserUsageRecordData>.Filter.Gte(r => r.CreatedAt, startDate),
                Builders<UserUsageRecordData>.Filter.Lt(r => r.CreatedAt, endDate)
            );

            return _userUsageCollection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<(List<UserUsageRecordData> Items, bool HasMore)> GetUserUsageHistoryPaginatedAsync(string userEmail, int limit, PaginationCursor<PaginationCursorNoFilterHelper>? cursor, bool fetchNext, List<long> businessIds = null)
        {
            var filterBuilder = Builders<UserUsageRecordData>.Filter;
            var baseFilter = filterBuilder.Eq(r => r.BusinessMasterUserEmail, userEmail);
            if (businessIds != null && businessIds.Count > 0)
            {
                baseFilter = filterBuilder.And(baseFilter, filterBuilder.In(r => r.BusinessId, businessIds));
            }

            FilterDefinition<UserUsageRecordData> finalFilter = baseFilter;
            SortDefinition<UserUsageRecordData> sortDefinition;

            if (fetchNext)
            {
                sortDefinition = Builders<UserUsageRecordData>.Sort.Descending(r => r.CreatedAt).Descending(r => r.Id);
                if (cursor != null)
                {
                    finalFilter = filterBuilder.And(baseFilter, filterBuilder.Or(
                        filterBuilder.Lt(r => r.CreatedAt, cursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(r => r.CreatedAt, cursor.Timestamp),
                            filterBuilder.Lt(r => r.Id, cursor.Id))
                    ));
                }
            }
            else // Fetch Previous
            {
                sortDefinition = Builders<UserUsageRecordData>.Sort.Ascending(r => r.CreatedAt).Ascending(r => r.Id);
                if (cursor != null)
                {
                    finalFilter = filterBuilder.And(baseFilter, filterBuilder.Or(
                        filterBuilder.Gt(r => r.CreatedAt, cursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(r => r.CreatedAt, cursor.Timestamp),
                            filterBuilder.Gt(r => r.Id, cursor.Id))
                    ));
                }
                else return (new List<UserUsageRecordData>(), false);
            }

            var queryLimit = limit + 1;
            var records = await _userUsageCollection.Find(finalFilter).Sort(sortDefinition).Limit(queryLimit).ToListAsync();

            bool hasMore = records.Count > limit;
            if (hasMore) records = records.Take(limit).ToList();
            if (!fetchNext) records.Reverse();

            return (records, hasMore);
        }

        // Dynamic Aggregation Methods
        public async Task<List<UserUsageMainStatsResult>> GetUserUsageMainStatsAsync(string masterUserEmail, DateTime startDate, DateTime endDate)
        {
            var pipeline = new BsonDocument[]
            {
            new BsonDocument("$match", new BsonDocument
            {
                { "BusinessMasterUserEmail", masterUserEmail },
                { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
            }),
            new BsonDocument("$unwind", "$ConsumedFeatures"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "businessId", "$BusinessId" },
                        { "featureKey", "$ConsumedFeatures.FeatureKey" },
                        { "consumedType", "$ConsumedFeatures.Type" },
                        { "sourceType", "$SourceType" }
                    }
                },
                { "totalQuantity", new BsonDocument("$sum", "$ConsumedFeatures.Quantity") },
                { "totalCost", new BsonDocument("$sum", "$ConsumedFeatures.TotalUsage") },
                { "count", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "BusinessId", "$_id.businessId" },
                { "FeatureKey", "$_id.featureKey" },
                { "ConsumedType", "$_id.consumedType" },
                { "SourceType", "$_id.sourceType" },
                { "TotalQuantity", "$totalQuantity" },
                { "TotalCost", "$totalCost" },
                { "Count", "$count" }
            })
            };

            return await _userUsageCollection.Aggregate<UserUsageMainStatsResult>(pipeline).ToListAsync();
        }

        public async Task<List<UserUsageUniqueSourceCountResult>> GetUserUsageUniqueSourceCountsAsync(string masterUserEmail, DateTime startDate, DateTime endDate)
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "BusinessMasterUserEmail", masterUserEmail },
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "businessId", "$BusinessId" },
                            { "sourceType", "$SourceType" }
                        }
                    },
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "BusinessId", "$_id.businessId" },
                    { "SourceType", "$_id.sourceType" },
                    { "Count", "$count" }
                })
            };
            return await _userUsageCollection.Aggregate<UserUsageUniqueSourceCountResult>(pipeline).ToListAsync();
        }

        public async Task<List<UserUsageAggregatedChartDataResult>> GetUserUsageAggregatedChartDataAsync(string masterUserEmail, DateTime startDate, DateTime endDate, string groupByFormat, string valueField)
        {
            var pipeline = new BsonDocument[]
            {
            new BsonDocument("$match", new BsonDocument
            {
                { "BusinessMasterUserEmail", masterUserEmail },
                { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
            }),
            new BsonDocument("$unwind", "$ConsumedFeatures"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "period", new BsonDocument("$dateToString", new BsonDocument { { "format", groupByFormat }, { "date", "$CreatedAt" } }) },
                        { "businessId", "$BusinessId" },
                        { "featureKey", "$ConsumedFeatures.FeatureKey" },
                        { "consumedType", "$ConsumedFeatures.Type" }
                    }
                },
                { "value", new BsonDocument("$sum", valueField) }
            }),
            new BsonDocument("$project", new BsonDocument
            {
                { "_id", 0 },
                { "Period", "$_id.period" },
                { "BusinessId", "$_id.businessId" },
                { "FeatureKey", "$_id.featureKey" },
                { "ConsumedType", "$_id.consumedType" },
                { "Value", "$value" }
            })
            };
            return await _userUsageCollection.Aggregate<UserUsageAggregatedChartDataResult>(pipeline).ToListAsync();
        }

        public async Task<List<UserUsageAggregatedSourceCountByPeriodResult>> GetAggregatedSourceCountByPeriodAsync(string masterUserEmail, DateTime startDate, DateTime endDate, string groupByFormat, UserUsageSourceTypeEnum sourceType)
        {
            var pipeline = new BsonDocument[]
            {
                // Stage 1: Filter efficiently by the exact source type we want to count
                new BsonDocument("$match", new BsonDocument
                {
                    { "BusinessMasterUserEmail", masterUserEmail },
                    { "SourceType", (int)sourceType }, // Filter for 'Conversation'
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                // Stage 2: Group by period and business, collecting unique source IDs
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "period", new BsonDocument("$dateToString", new BsonDocument { { "format", groupByFormat }, { "date", "$CreatedAt" } }) },
                            { "businessId", "$BusinessId" }
                        }
                    },
                    { "uniqueSourceIds", new BsonDocument("$addToSet", "$SourceId") }
                }),
                // Stage 3: Project the final count from the size of the unique set
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "Period", "$_id.period" },
                    { "BusinessId", "$_id.businessId" },
                    { "Count", new BsonDocument("$size", "$uniqueSourceIds") }
                })
            };
            return await _userUsageCollection.Aggregate<UserUsageAggregatedSourceCountByPeriodResult>(pipeline).ToListAsync();
        }

        public async Task<Dictionary<UserUsageSourceTypeEnum, long>> GetUserUsageSourceTypeCountsAsync(string userEmail, DateTime startDate, DateTime endDate, List<long>? businessIds = null)
        {
            var filterBuilder = Builders<UserUsageRecordData>.Filter;
            var filter = filterBuilder.And(
                filterBuilder.Eq(r => r.BusinessMasterUserEmail, userEmail),
                filterBuilder.Gte(r => r.CreatedAt, startDate),
                filterBuilder.Lt(r => r.CreatedAt, endDate)
            );

            if (businessIds != null && businessIds.Count > 0)
            {
                filter = filterBuilder.And(
                    filter,
                    filterBuilder.In(r => r.BusinessId, businessIds)
                );
            }

            var aggregationResult = await _userUsageCollection.Aggregate()
                .Match(filter)
                .Group(r => r.SourceType,
                       g => new
                       {
                           SourceType = g.Key, 
                           Count = g.LongCount()
                       })
                .ToListAsync();

            return aggregationResult.ToDictionary(
                doc => doc.SourceType,
                doc => doc.Count
            );
        }

        public async Task<List<OverallUsageResult>> GetOverallUsageCount(DateTime startDate, DateTime endDate)
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                new BsonDocument("$unwind", "$ConsumedFeatures"),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "featureKey", "$ConsumedFeatures.FeatureKey" },
                            { "consumedType", "$ConsumedFeatures.Type" },
                            { "sourceType", "$SourceType" }
                        }
                    },
                    { "totalQuantity", new BsonDocument("$sum", "$ConsumedFeatures.Quantity") },
                    { "totalCost", new BsonDocument("$sum", "$ConsumedFeatures.TotalUsage") },
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "FeatureKey", "$_id.featureKey" },
                    { "ConsumedType", "$_id.consumedType" },
                    { "SourceType", "$_id.sourceType" },
                    { "TotalQuantity", "$totalQuantity" },
                    { "TotalCost", "$totalCost" },
                    { "Count", "$count" }
                })
            };

            return await _userUsageCollection.Aggregate<OverallUsageResult>(pipeline).ToListAsync();
        }
    }
}
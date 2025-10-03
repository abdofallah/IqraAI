using IqraCore.Entities.Helpers;
using IqraCore.Entities.Usage;
using IqraCore.Entities.User.Usage;
using IqraCore.Entities.User.Usage.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.User
{
    public class UserUsageRepository
    {
        private readonly ILogger<UserUsageRepository> _logger;
        private readonly IMongoCollection<UserUsageRecordData> _userUsageCollection;

        public UserUsageRepository(ILogger<UserUsageRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            var database = client.GetDatabase(databaseName);
            _userUsageCollection = database.GetCollection<UserUsageRecordData>("UserUsageRecords");

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

        public Task AddUserUsageRecordAsync(UserUsageRecordData record, IClientSessionHandle session)
        {
            return _userUsageCollection.InsertOneAsync(session, record);
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
        public async Task<OverallUserUsageStatsByTypeResult> GetOverallUserUsageStatsByTypeAsync(string userEmail, DateTime startDate, DateTime endDate)
        {
            var pipeline = new BsonDocument[]
            {
                // Match the relevant user records and time frame.
                new BsonDocument("$match", new BsonDocument {
                    { "BusinessMasterUserEmail", userEmail },
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                new BsonDocument("$unwind", "$ConsumedFeatures"),
                // Group by both FeatureKey and the consumption Type
                new BsonDocument("$group", new BsonDocument {
                    { "_id", new BsonDocument {
                        { "featureKey", "$ConsumedFeatures.FeatureKey" },
                        { "type", "$ConsumedFeatures.Type" }
                    }},
                    { "TotalQuantity", new BsonDocument("$sum", "$ConsumedFeatures.Quantity") },
                    { "TotalCost", new BsonDocument("$sum", "$ConsumedFeatures.TotalUsage") }
                }),
                // Group again by just the feature to bundle the types together
                new BsonDocument("$group", new BsonDocument {
                    { "_id", "$_id.featureKey" },
                    { "TotalFeatureCost", new BsonDocument("$sum", "$TotalCost") },
                    { "Breakdown", new BsonDocument("$push", new BsonDocument {
                        { "type", "$_id.type" },
                        { "quantity", "$TotalQuantity" }
                    })}
                }),
                // Final grouping to get the grand total and structure the final object
                new BsonDocument("$group", new BsonDocument {
                    { "_id", BsonNull.Value },
                    { "GrandTotalCost", new BsonDocument("$sum", "$TotalFeatureCost") },
                    { "Features", new BsonDocument("$push", new BsonDocument {
                        { "k", "$_id" }, // Feature Key
                        { "v", new BsonDocument("breakdown", "$Breakdown") }
                    })}
                }),
                new BsonDocument("$project", new BsonDocument {
                    { "_id", 0 },
                    { "TotalCost", "$GrandTotalCost" },
                    { "UsageByFeature", new BsonDocument("$arrayToObject", "$Features") }
                })
            };

            var result = await _userUsageCollection.Aggregate<OverallUserUsageStatsByTypeResult>(pipeline).FirstOrDefaultAsync();
            return result ?? new OverallUserUsageStatsByTypeResult(); // Return an empty stats object if no usage is found
        }

        public Task<List<AggregatedUsageStatsResult>> GetAggregatedUserUsageByPeriodAsync(string userEmail, DateTime startDate, DateTime endDate, string groupByFormat)
        {
            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument {
                    { "BusinessMasterUserEmail", userEmail },
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                new BsonDocument("$unwind", "$ConsumedFeatures"),

                // Stage 1: Group by the time period AND the feature key
                new BsonDocument("$group", new BsonDocument {
                    { "_id", new BsonDocument {
                        { "period", new BsonDocument("$dateToString", new BsonDocument {
                            { "format", groupByFormat }, { "date", "$CreatedAt" }, { "timezone", "UTC" }
                        })},
                        { "businessId", "$BusinessId" },
                        { "featureKey", "$ConsumedFeatures.FeatureKey" }
                    }},
                    { "TotalQuantity", new BsonDocument("$sum", "$ConsumedFeatures.Quantity") },
                    { "TotalCost", new BsonDocument("$sum", "$ConsumedFeatures.TotalUsage") }
                }),

                // Stage 2: Group again by just the period to bundle the features together
                new BsonDocument("$group", new BsonDocument {
                    { "_id", new BsonDocument {
                        { "period", "$_id.period" },
                        { "businessId", "$_id.businessId" }
                    }},
                    { "TotalCost", new BsonDocument("$sum", "$TotalCost") },
                    { "Features", new BsonDocument("$push", new BsonDocument {
                        { "k", "$_id.featureKey" },
                        { "v", "$TotalQuantity" }
                    })}
                }),

                // Stage 3: Project to the final shape with the dictionary
                new BsonDocument("$project", new BsonDocument {
                    { "TotalCost", 1 },
                    { "UsageByFeature", new BsonDocument("$arrayToObject", "$Features") }
                }),
                new BsonDocument("$sort", new BsonDocument { { "_id.period", 1 } })
            };

            return _userUsageCollection.Aggregate<AggregatedUsageStatsResult>(pipeline).ToListAsync();
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
    }
}
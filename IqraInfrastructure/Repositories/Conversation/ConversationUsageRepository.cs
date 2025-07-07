using IqraCore.Entities.Billing.Usage;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Usage;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Conversation
{
    public class ConversationUsageRepository
    {
        private readonly ILogger<ConversationUsageRepository> _logger;
        private readonly IMongoCollection<BaseMinuteUsageRecord> _usageCollection;

        public ConversationUsageRepository(ILogger<ConversationUsageRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            var database = client.GetDatabase(databaseName);
            _usageCollection = database.GetCollection<BaseMinuteUsageRecord>("MinuteUsageRecords");

            // It's crucial to create indexes for efficient querying
            var indexKeysDefinition = Builders<BaseMinuteUsageRecord>.IndexKeys
                .Ascending(r => r.MasterUserEmail)
                .Ascending(r => r.CreatedAt);
            _usageCollection.Indexes.CreateOneAsync(new CreateIndexModel<BaseMinuteUsageRecord>(indexKeysDefinition));
        }

        public Task AddUsageRecordAsync(BaseMinuteUsageRecord record)
        {
            return _usageCollection.InsertOneAsync(record);
        }

        // We will need this for the "Usage Details" chart later
        public Task<List<BaseMinuteUsageRecord>> GetUsageForUserAsync(string masterUserEmail, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<BaseMinuteUsageRecord>.Filter.And(
                Builders<BaseMinuteUsageRecord>.Filter.Eq(r => r.MasterUserEmail, masterUserEmail),
                Builders<BaseMinuteUsageRecord>.Filter.Gte(r => r.CreatedAt, startDate),
                Builders<BaseMinuteUsageRecord>.Filter.Lt(r => r.CreatedAt, endDate)
            );

            return _usageCollection.Find(filter).ToListAsync();
        }

        // Overload for use within a transaction
        public Task AddUsageRecordAsync(BaseMinuteUsageRecord record, IClientSessionHandle session)
        {
            return _usageCollection.InsertOneAsync(session, record);
        }

        // get overall stats for the entire period
        public async Task<OverallUsageStatsResult?> GetOverallUsageStatsAsync(string masterUserEmail, DateTime startDate, DateTime endDate)
        {
            var pipeline = new BsonDocument[]
            {
                // 1. Match documents for the user and time frame
                new BsonDocument("$match", new BsonDocument
                {
                    { "MasterUserEmail", masterUserEmail },
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                // 2. Group all matched documents into a single result
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", BsonNull.Value }, // Group everything together
                    { "TotalMinutes", new BsonDocument("$sum", "$TotalMinutesUsed") },
                    { "TotalCost", new BsonDocument("$sum", "$TotalCost") },
                    { "TotalCalls", new BsonDocument("$sum", 1) } // Count each document as one call
                })
            };

            var result = await _usageCollection.Aggregate<OverallUsageStatsResult>(pipeline).FirstOrDefaultAsync();
            return result;
        }


        // get stats grouped by a time unit (hour, day, etc.)
        public Task<List<AggregatedUsageStatsResult>> GetAggregatedUsageByPeriodAsync(string masterUserEmail, DateTime startDate, DateTime endDate, string groupByFormat)
        {
            var pipeline = new BsonDocument[]
            {
                // 1. Match documents for the user and time frame
                new BsonDocument("$match", new BsonDocument
                {
                    { "MasterUserEmail", masterUserEmail },
                    { "CreatedAt", new BsonDocument { { "$gte", startDate }, { "$lt", endDate } } }
                }),
                // 2. Group by the specified date format and calculate sums
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument("$dateToString", new BsonDocument
                        {
                            { "format", groupByFormat },
                            { "date", "$CreatedAt" },
                            { "timezone", "UTC" } // Important for consistency
                        })
                    },
                    { "TotalMinutes", new BsonDocument("$sum", "$TotalMinutesUsed") },
                    { "TotalCost", new BsonDocument("$sum", "$TotalCost") },
                    { "TotalCalls", new BsonDocument("$sum", 1) }
                }),
                // 3. Sort by the group key (date/hour)
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            return _usageCollection.Aggregate<AggregatedUsageStatsResult>(pipeline).ToListAsync();
        }


        public async Task<(List<BaseMinuteUsageRecord> Items, bool HasMore)> GetUsageHistoryPaginatedAsync(string masterUserEmail, int limit, PaginationCursor? cursor, bool fetchNext)
        {
            var filterBuilder = Builders<BaseMinuteUsageRecord>.Filter;
            var baseFilter = filterBuilder.Eq(r => r.MasterUserEmail, masterUserEmail);

            // The rest of this method's logic is identical to your reference implementation.
            // I've adapted it for MinuteUsageRecord.
            FilterDefinition<BaseMinuteUsageRecord> finalFilter = baseFilter;
            SortDefinition<BaseMinuteUsageRecord> sortDefinition;

            if (fetchNext)
            {
                sortDefinition = Builders<BaseMinuteUsageRecord>.Sort
                    .Descending(r => r.CreatedAt)
                    .Descending(r => r.Id);

                if (cursor != null)
                {
                    var cursorFilter = filterBuilder.Or(
                        filterBuilder.Lt(r => r.CreatedAt, cursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(r => r.CreatedAt, cursor.Timestamp),
                            filterBuilder.Lt(r => r.Id, cursor.Id)
                        )
                    );
                    finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                }
            }
            else
            {
                sortDefinition = Builders<BaseMinuteUsageRecord>.Sort
                    .Ascending(r => r.CreatedAt)
                    .Ascending(r => r.Id);

                if (cursor != null)
                {
                    var cursorFilter = filterBuilder.Or(
                        filterBuilder.Gt(r => r.CreatedAt, cursor.Timestamp),
                        filterBuilder.And(
                            filterBuilder.Eq(r => r.CreatedAt, cursor.Timestamp),
                            filterBuilder.Gt(r => r.Id, cursor.Id)
                        )
                    );
                    finalFilter = filterBuilder.And(baseFilter, cursorFilter);
                }
                else
                {
                    return (new List<BaseMinuteUsageRecord>(), false);
                }
            }

            var queryLimit = limit + 1;
            var records = await _usageCollection.Find(finalFilter)
                .Sort(sortDefinition)
                .Limit(queryLimit)
                .ToListAsync();

            bool hasMore = records.Count > limit;
            if (hasMore)
            {
                records = records.Take(limit).ToList();
            }
            if (!fetchNext)
            {
                records.Reverse();
            }

            return (records, hasMore);
        }
    }
}
using IqraCore.Entities.Business;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessToolTestExecutionRepository
    {
        private readonly ILogger<BusinessToolTestExecutionRepository> _logger;
        private readonly IMongoCollection<BusinessToolTestExecution> _collection;

        public BusinessToolTestExecutionRepository(ILogger<BusinessToolTestExecutionRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase("IqraBusinessApp");
            _collection = database.GetCollection<BusinessToolTestExecution>("BusinessToolTestExecution");

            // Create TTL index for auto-expiry after 30 days
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            try
            {
                var ttlIndex = new CreateIndexModel<BusinessToolTestExecution>(
                    Builders<BusinessToolTestExecution>.IndexKeys.Ascending(x => x.ExecutedAt),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(30), Name = "ttl_30days" }
                );

                var businessToolIndex = new CreateIndexModel<BusinessToolTestExecution>(
                    Builders<BusinessToolTestExecution>.IndexKeys
                        .Ascending(x => x.BusinessId)
                        .Ascending(x => x.ToolId)
                        .Descending(x => x.ExecutedAt),
                    new CreateIndexOptions { Name = "idx_business_tool_date" }
                );

                _collection.Indexes.CreateMany(new[] { ttlIndex, businessToolIndex });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for BusinessToolTestExecution collection.");
            }
        }

        public async Task<bool> CreateAsync(BusinessToolTestExecution execution)
        {
            try
            {
                await _collection.InsertOneAsync(execution);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting tool test execution for tool {ToolId} in business {BusinessId}.", execution.ToolId, execution.BusinessId);
                return false;
            }
        }

        public async Task<List<BusinessToolTestExecution>> GetByToolIdAsync(long businessId, string toolId, int limit = 20)
        {
            var filter = Builders<BusinessToolTestExecution>.Filter.And(
                Builders<BusinessToolTestExecution>.Filter.Eq(x => x.BusinessId, businessId),
                Builders<BusinessToolTestExecution>.Filter.Eq(x => x.ToolId, toolId)
            );

            return await _collection.Find(filter)
                .SortByDescending(x => x.ExecutedAt)
                .Limit(limit)
                .ToListAsync();
        }
    }
}

using IqraCore.Entities.Region;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Region
{
    public class RegionRepository
    {
        private ILogger<RegionRepository>? _logger;
        private readonly IMongoCollection<RegionData> _regionCollection;

        private readonly string DatabaseName = "IqraApp";
        private readonly string CollectionName = "Regions";

        public RegionRepository(IMongoClient client)
        {
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            _regionCollection = database.GetCollection<RegionData>(CollectionName);
        }

        public void SetLogger(ILogger<RegionRepository> logger) => _logger = logger;

        // --- Region CRUD ---

        public async Task<bool> AddRegion(RegionData regionData)
        {
            await _regionCollection.InsertOneAsync(regionData);
            return true;
        }

        public async Task<List<RegionData>> GetRegions()
        {
            return await _regionCollection.Find(_ => true).ToListAsync();
        }

        public async Task<List<RegionData>> GetRegions(int page, int pageSize)
        {
            return await _regionCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<RegionData> GetRegion(string id)
        {
            return await _regionCollection.Find(x => x.RegionId == id).FirstOrDefaultAsync();
        }

        public async Task<RegionData?> GetRegionById(string regionId)
        {
            return await _regionCollection.Find(x => x.RegionId == regionId).FirstOrDefaultAsync();
        }

        public async Task<bool> CheckRegionExists(string regionId)
        {
            return await _regionCollection.Find(x => x.RegionId == regionId).AnyAsync();
        }

        public async Task<bool> DeleteRegion(string regionId)
        {
            var result = await _regionCollection.DeleteOneAsync(x => x.RegionId == regionId);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateRegion(string id, UpdateDefinition<RegionData> updateDefinition)
        {
            var result = await _regionCollection.UpdateOneAsync(x => x.RegionId == id, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateRegion(FilterDefinition<RegionData> filterDefinition, UpdateDefinition<RegionData> updateDefinition)
        {
            var result = await _regionCollection.UpdateOneAsync(filterDefinition, updateDefinition);
            return result.ModifiedCount > 0;
        }

        // --- Server Management (Nested Array) ---

        public async Task<bool> AddRegionServer(string regionId, RegionServerData server)
        {
            var filter = Builders<RegionData>.Filter.Eq(x => x.RegionId, regionId);
            var update = Builders<RegionData>.Update.Push(x => x.Servers, server);

            var result = await _regionCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteRegionServer(string regionId, string serverId)
        {
            var filter = Builders<RegionData>.Filter.Eq(x => x.RegionId, regionId);
            var update = Builders<RegionData>.Update.PullFilter(x => x.Servers, s => s.Id == serverId);

            var result = await _regionCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
    }
}
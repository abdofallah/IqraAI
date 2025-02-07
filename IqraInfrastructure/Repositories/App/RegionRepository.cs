using IqraCore.Entities.Region;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.App
{
    public class RegionRepository
    {
        private readonly string CollectionName = "Regions";

        private readonly IMongoCollection<RegionData> _regionCollection;

        public RegionRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _regionCollection = database.GetCollection<RegionData>(CollectionName);
        }

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

        public async Task<RegionData> GetRegion(string countryCode, string countryRegion)
        {
            return await _regionCollection.Find(x => x.CountryCode == countryCode && x.CountryRegion == countryRegion).FirstOrDefaultAsync();
        }

        public async Task<RegionData> GetRegion(string id)
        {
            return await _regionCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateRegion(string id, UpdateDefinition<RegionData> updateDefinition)
        {
            var result = await _regionCollection.UpdateOneAsync(x => x.Id == id, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateRegion(string countryCode, string countryRegion, UpdateDefinition<RegionData> updateDefinition)
        {
            var result = await _regionCollection.UpdateOneAsync(x => x.CountryCode == countryCode && x.CountryRegion == countryRegion, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateRegion(FilterDefinition<RegionData> filterDefinition, UpdateDefinition<RegionData> updateDefinition)
        {
            var result = await _regionCollection.UpdateOneAsync(filterDefinition, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<RegionData?> GetRegionById(string regionId)
        {
            return await _regionCollection.Find(x => x.Id == regionId).FirstOrDefaultAsync();
        }
    }
}

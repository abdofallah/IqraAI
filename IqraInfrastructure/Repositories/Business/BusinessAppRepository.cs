using IqraCore.Entities.Business;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessAppRepository
    {
        private readonly string CollectionName = "BusinessApp";

        private readonly IMongoCollection<BusinessApp> _businessAppCollection;

        public BusinessAppRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessAppCollection = database.GetCollection<BusinessApp>(CollectionName);
        }

        public Task<List<BusinessApp>> GetBusinessesAppAsync()
        {
            return _businessAppCollection.Find(_ => true).ToListAsync();
        }

        public Task<BusinessApp?> GetBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            return _businessAppCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task AddBusinessAppAsync(BusinessApp businessApp)
        {
            return _businessAppCollection.InsertOneAsync(businessApp);
        }

        public async Task<bool> DeleteBusinessAppAsync(long businessId)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateBusinessAppAsync(long businessId, UpdateDefinition<BusinessApp> updateDefinition)
        {
            var filter = Builders<BusinessApp>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessAppCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }
    }
}
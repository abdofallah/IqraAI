using IqraCore.Entities.Business;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly string CollectionName = "Business";

        private readonly IMongoCollection<BusinessData> _businessCollection;

        public BusinessRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessCollection = database.GetCollection<BusinessData>(CollectionName);
        }
        public BusinessRepository(IMongoDatabase database)
        {
            _businessCollection = database.GetCollection<BusinessData>(CollectionName);
        }

        public Task<List<BusinessData>> GetBusinessesAsync()
        {
            return _businessCollection.Find(_ => true).ToListAsync();
        }

        public Task<BusinessData?> GetBusinessAsync(long businessId)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            return _businessCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task AddBusinessAsync(BusinessData business)
        {
            return _businessCollection.InsertOneAsync(business);
        }

        public async Task<bool> UpdateBusinessAsync(long businessId, UpdateDefinition<BusinessData> updateDefinition)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;

        }

        public async Task<bool> DeleteBusinessAsync(long businessId)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public Task<List<BusinessData>> GetBusinessesAsync(List<long> businessesId)
        {
            var filter = Builders<BusinessData>.Filter.In(b => b.Id, businessesId);
            return _businessCollection.Find(filter).ToListAsync();
        }

        public Task<List<BusinessData>> GetBusinessesByUserEmailAsync(string userEmail)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.UserEmail, userEmail);
            return _businessCollection.Find(filter).ToListAsync();
        }
    }
}
using IqraCore.Entities.Business;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessRepository
    {
        private static readonly string CollectionName = "Business";
        private static readonly string CounterCollectionName = CollectionName + "Counter";

        private static readonly string BusinessIdCounterField = "BusinessIdCounter";

        private readonly IMongoCollection<BusinessData> _businessCollection;
        private readonly IMongoCollection<BsonDocument> _businessCounterCollection;

        public BusinessRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessCollection = database.GetCollection<BusinessData>(CollectionName);
            _businessCounterCollection = database.GetCollection<BsonDocument>(CounterCollectionName);

            ValidateBusinessIdCounter().GetAwaiter().GetResult();
        }

        public async Task ValidateBusinessIdCounter()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BusinessIdCounterField);
            var counter = await _businessCounterCollection.Find(filter).FirstOrDefaultAsync();
            if (counter == null)
            {
                await _businessCounterCollection.InsertOneAsync(
                    new BsonDocument
                    {
                        { "_id", BusinessIdCounterField },
                        { "Counter", 0 }
                    }
                );
            }
        }

        public async Task<long> GetNextBusinessId()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BusinessIdCounterField);
            var update = Builders<BsonDocument>.Update.Inc("Counter", 1);

            var result = await _businessCounterCollection.FindOneAndUpdateAsync(filter, update);

            return result["Counter"].AsInt32;
        }

        public Task<List<BusinessData>> GetBusinessesAsync()
        {
            return _businessCollection.Find(_ => true).ToListAsync();
        }

        public Task<List<BusinessData>> GetBusinessesAsync(int page, int pageSize)
        {
            return _businessCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
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

        public Task<List<BusinessData>> GetBusinessesByMasterUserEmailAsync(string userEmail)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.MasterUserEmail, userEmail);
            return _businessCollection.Find(filter).ToListAsync();
        }

        public async Task<List<BusinessData>?> SearchBusinessesAsync(string query, int page, int pageSize)
        {
            var filter = Builders<BusinessData>.Filter.Regex(b => b.Name, new Regex(query, RegexOptions.IgnoreCase));
            if (long.TryParse(query, out var id))
            {
                filter = filter | Builders<BusinessData>.Filter.Eq(b => b.Id, id);
            }

            return await _businessCollection.Find(filter).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }
    }
}
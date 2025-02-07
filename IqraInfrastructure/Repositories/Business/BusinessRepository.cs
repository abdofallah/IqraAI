using IqraCore.Entities.Business;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
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

        public async Task<bool> ReplaceBusinessAsync(BusinessData businessDataBackup)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessDataBackup.Id);
            var result = await _businessCollection.ReplaceOneAsync(filter, businessDataBackup);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> AddBusinessSubUserAsync(long businessId, BusinessUser newSubUserData)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var update = Builders<BusinessData>.Update
                .Push(b => b.SubUsers, newSubUserData);
            var result = await _businessCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> ReplaceBusinessSubUserAsync(long businessId, BusinessUser newSubUserData)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var matchElementFilter = Builders<BusinessData>.Filter.ElemMatch(b => b.SubUsers, su => su.Email == newSubUserData.Email);

            var combinedFilter = Builders<BusinessData>.Filter.And(filter, matchElementFilter);

            var update = Builders<BusinessData>.Update.Set(b => b.SubUsers.FirstMatchingElement(), newSubUserData);

            var options = new UpdateOptions { IsUpsert = false };
            var result = await _businessCollection.UpdateOneAsync(combinedFilter, update, options);

            return result.ModifiedCount > 0;
        }

        public async Task<List<string>> GetBusinessLanguages(long businessId)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var projection = Builders<BusinessData>.Projection.Include(b => b.Languages).Include(b => b.Id);

            var business = await _businessCollection.Find(filter).Project<BusinessData>(projection).FirstOrDefaultAsync();
            return business.Languages;
        }

        public async Task<bool> CheckBusinessExists(long businessId)
        {
            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var business = await _businessCollection.Find(filter).FirstOrDefaultAsync();
            return business != null;
        }

        public async Task<bool> addNumberIdToBusiness(string numberId, long businessId)
        {
            var updateDefinition = Builders<BusinessData>.Update
                .AddToSet(b => b.NumberIds, numberId);

            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> removeNumberIdFromBusiness(string numberId, long businessId)
        {
            var updateDefinition = Builders<BusinessData>.Update
                .Pull(b => b.NumberIds, numberId);

            var filter = Builders<BusinessData>.Filter.Eq(b => b.Id, businessId);
            var result = await _businessCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }
    }
}
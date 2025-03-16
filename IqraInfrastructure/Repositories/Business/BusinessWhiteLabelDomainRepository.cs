using IqraCore.Entities.Business.WhiteLabelDomain;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Business
{
    public class BusinessWhiteLabelDomainRepository
    {
        private readonly ILogger<BusinessWhiteLabelDomainRepository> _logger;

        private static readonly string CollectionName = "BusinessWhiteLabelDomain";
        private static readonly string CounterCollectionName = CollectionName + "Counter";

        private static readonly string BusinessWhiteLabelDomainIdCounterField = "BusinessWhiteLabelDomainIdCounter";

        private readonly IMongoCollection<BusinessWhiteLabelDomain> _businessWhiteLabelDomainCollection;
        private readonly IMongoCollection<BsonDocument> _businessWhiteLabelDomainCounterCollection;

        public BusinessWhiteLabelDomainRepository(ILogger<BusinessWhiteLabelDomainRepository> logger, string connectionString, string databaseName)
        {
            _logger = logger;

            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _businessWhiteLabelDomainCollection = database.GetCollection<BusinessWhiteLabelDomain>(CollectionName);
            _businessWhiteLabelDomainCounterCollection = database.GetCollection<BsonDocument>(CounterCollectionName);

            ValidateBusinessWhiteLabelDomainIdCounter().GetAwaiter().GetResult();
        }

        public async Task ValidateBusinessWhiteLabelDomainIdCounter()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BusinessWhiteLabelDomainIdCounterField);
            var counter = await _businessWhiteLabelDomainCounterCollection.Find(filter).FirstOrDefaultAsync();
            if (counter == null)
            {
                await _businessWhiteLabelDomainCounterCollection.InsertOneAsync(
                    new BsonDocument
                    {
                        { "_id", BusinessWhiteLabelDomainIdCounterField },
                        { "Counter", 0 }
                    }
                );
            }
        }

        public async Task<long> GetNextBusinessWhiteLabelDomainId()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BusinessWhiteLabelDomainIdCounterField);
            var update = Builders<BsonDocument>.Update.Inc("Counter", 1);

            var result = await _businessWhiteLabelDomainCounterCollection.FindOneAndUpdateAsync(filter, update);

            return result["Counter"].AsInt32;
        }

        public Task<List<BusinessWhiteLabelDomain>> GetBusinessesWhiteLabelDomainAsync()
        {
            return _businessWhiteLabelDomainCollection.Find(_ => true).ToListAsync();
        }

        public Task<BusinessWhiteLabelDomain?> GetBusinessWhiteLabelDomainAsync(long id)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.Eq(b => b.Id, id);
            return _businessWhiteLabelDomainCollection.Find(filter).FirstOrDefaultAsync();
        }

        public Task<List<BusinessWhiteLabelDomain>?> GetBusinessWhiteLabelDomainsAsync(List<long> ids)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.In(b => b.Id, ids);
            return _businessWhiteLabelDomainCollection.Find(filter).ToListAsync();
        }

        public Task<List<BusinessWhiteLabelDomain>?> GetBusinessWhiteLabelDomainsByBusinessIdAsync(long businessId)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.Eq(b => b.BusinessId, businessId);
            return _businessWhiteLabelDomainCollection.Find(filter).ToListAsync();
        }

        public Task AddBusinessWhiteLabelDomainAsync(BusinessWhiteLabelDomain domainData)
        {
            return _businessWhiteLabelDomainCollection.InsertOneAsync(domainData);
        }

        public async Task<bool> DeleteBusinessWhiteLabelDomainAsync(long id)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.Eq(b => b.Id, id);
            var result = await _businessWhiteLabelDomainCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateBusinessWhiteLabelDomainAsync(long id, UpdateDefinition<BusinessWhiteLabelDomain> updateDefinition)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.Eq(b => b.Id, id);
            var result = await _businessWhiteLabelDomainCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessWhiteLabelDomainAsync(BusinessWhiteLabelDomain businessWhiteLabelDomain)
        {
            var filter = Builders<BusinessWhiteLabelDomain>.Filter.Eq(b => b.Id, businessWhiteLabelDomain.Id);
            var result = await _businessWhiteLabelDomainCollection.ReplaceOneAsync(filter, businessWhiteLabelDomain);
            return result.ModifiedCount > 0;
        }
    }
}

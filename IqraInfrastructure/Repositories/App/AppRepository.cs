using IqraCore.Entities.App.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.App
{
    public class AppRepository
    {
        private readonly ILogger<AppRepository> _logger;

        private readonly string CollectionName = "AppConfiguration";

        private readonly IMongoCollection<BsonDocument> _applicationConfigurationCollection;

        public AppRepository(ILogger<AppRepository> logger, string connectionString, string databaseName)
        {
            _logger = logger;

            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _applicationConfigurationCollection = database.GetCollection<BsonDocument>(CollectionName);
        }

        /**
         * 
         * API Keys
         * 
        **/

        private const string ApiKeyField = "ApiKeys";
        private const string VestaCPProxyTemplatesHash = "VestaCPProxyTemplatesHash";

        /**
         * 
         * API Keys 
         * 
        **/ 
        public async Task<bool> AddApiKey(string apiKey)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ApiKeyField);
            var update = Builders<BsonDocument>.Update.Push(ApiKeyField,
                BsonDocument.Create(new ApiKey
                {
                    Key = apiKey,
                    CreatedAt = DateTime.UtcNow
                })
            );

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0;
        }

        public async Task<ApiKey?> GetApiKeyData(string apiKey)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ApiKeyField) &
                         Builders<BsonDocument>.Filter.ElemMatch(ApiKeyField, Builders<ApiKey>.Filter.Eq(d => d.Key, apiKey));

            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null) return null;

            return BsonSerializer.Deserialize<ApiKey>(result);
        }

        public async Task<bool> ApiKeyExists(string apiKey)
        {
            var result = GetApiKeyData(apiKey);

            return result != null;
        }

        /**
         * 
         * VestaCP Proxy Templates Hash
         * 
        **/

        public async Task<bool> AddVestaCPProxyTemplatesHash(Dictionary<string, string> templateHashes)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", VestaCPProxyTemplatesHash);
            var update = Builders<BsonDocument>.Update.Set("TemplateHashes", templateHashes);

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }

        public async Task<VestaCPProxyTemplateHashes?> GetVestaCPProxyTemplatesHash()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", VestaCPProxyTemplatesHash);

            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null) return null;

            return BsonSerializer.Deserialize<VestaCPProxyTemplateHashes>(result);
        }
    }
}

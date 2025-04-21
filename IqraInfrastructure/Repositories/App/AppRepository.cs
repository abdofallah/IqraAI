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

        // Fields
        private const string AppPermissionConfigField = "AppPermissionConfig";
        private const string VestaCPProxyTemplatesHash = "VestaCPProxyTemplatesHash";

        /**
         * 
         * API Keys 
         * 
        **/ 
        public async Task<bool> AddUpdateAppPermissionConfig(AppPermissionConfig appPermissionConfig)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", AppPermissionConfigField);
            var update = Builders<BsonDocument>.Update.Set(AppPermissionConfigField,
                BsonDocument.Create(appPermissionConfig)
            );

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0;
        }

        public async Task<AppPermissionConfig?> GetAppPermissionConfig()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", AppPermissionConfigField);

            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null) return null;

            return BsonSerializer.Deserialize<AppPermissionConfig>(result);
        }
        /**
         * 
         * VestaCP Proxy Templates Hash
         * 
        **/

        public async Task<bool> AddUpdateVestaCPProxyTemplatesHash(Dictionary<string, string> templateHashes)
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

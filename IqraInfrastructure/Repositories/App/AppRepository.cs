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

        private readonly string DatabaseName = "IqraApp";
        private readonly string CollectionName = "AppConfiguration";

        private readonly IMongoCollection<BsonDocument> _applicationConfigurationCollection;

        public AppRepository(ILogger<AppRepository> logger, IMongoClient client)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(DatabaseName);
            _applicationConfigurationCollection = database.GetCollection<BsonDocument>(CollectionName);
        }

        // Fields
        private const string IqraAppConfigConfigField = "IqraAppConfig";
        private const string AppPermissionConfigField = "AppPermissionConfig";
        private const string EmailTemplatesField = "EmailTemplates";

        /**
         * 
         * App Permission Config
         * 
        **/
        public async Task<bool> AddUpdateIqraAppConfig(IqraAppConfig iqraAppConfig)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", IqraAppConfigConfigField);

            var result = await _applicationConfigurationCollection.ReplaceOneAsync(filter, iqraAppConfig.ToBsonDocument(), new ReplaceOptions { IsUpsert = true });
            return result.IsAcknowledged && (result.UpsertedId.IsString || result.ModifiedCount > 0);
        }

        public async Task<IqraAppConfig?> GetIqraAppConfig()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", IqraAppConfigConfigField);

            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null) return null;

            return BsonSerializer.Deserialize<IqraAppConfig>(result);
        } 

        /**
         * 
         * App Permission Config
         * 
        **/ 
        public async Task<bool> AddUpdateAppPermissionConfig(AppPermissionConfig appPermissionConfig)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", AppPermissionConfigField);

            var result = await _applicationConfigurationCollection.ReplaceOneAsync(filter, appPermissionConfig.ToBsonDocument(), new ReplaceOptions { IsUpsert = true });
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
         * Email Templates
         * 
        **/
        public async Task<bool> AddUpdateEmailTemplates(EmailTemplates emailTemplates)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", EmailTemplatesField);

            var result = await _applicationConfigurationCollection.ReplaceOneAsync(filter, emailTemplates.ToBsonDocument(), new ReplaceOptions { IsUpsert = true });
            return result.IsAcknowledged && result.ModifiedCount > 0;
        }
        
        public async Task<EmailTemplates?> GetEmailTemplates()
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", EmailTemplatesField);
                var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

                if (result == null) return null;

                return BsonSerializer.Deserialize<EmailTemplates>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email templates");
                return null;
            }
        }
    }
}

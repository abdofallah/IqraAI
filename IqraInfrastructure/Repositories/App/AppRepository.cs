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

        public AppRepository(ILogger<AppRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;
            IMongoDatabase database = client.GetDatabase(databaseName);
            _applicationConfigurationCollection = database.GetCollection<BsonDocument>(CollectionName);
        }

        // Fields
        private const string AppPermissionConfigField = "AppPermissionConfig";
        private const string VestaCPProxyTemplatesHashField = "VestaCPProxyTemplatesHash";
        private const string EmailTemplatesField = "EmailTemplates";
        private const string BillingPlanConfigField = "BillingPlanConfig";

        /**
         * 
         * App Permission Config
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
            var filter = Builders<BsonDocument>.Filter.Eq("_id", VestaCPProxyTemplatesHashField);
            var update = Builders<BsonDocument>.Update.Set("TemplateHashes", templateHashes);

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }

        public async Task<VestaCPProxyTemplateHashes?> GetVestaCPProxyTemplatesHash()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", VestaCPProxyTemplatesHashField);

            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();

            if (result == null) return null;

            return BsonSerializer.Deserialize<VestaCPProxyTemplateHashes>(result);
        }

        /**
         * 
         * Email Templates
         * 
        **/
        public async Task<bool> AddUpdateEmailTemplates(EmailTemplates emailTemplates)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", EmailTemplatesField);
            var update = Builders<BsonDocument>.Update.Set(EmailTemplatesField,
                BsonDocument.Create(emailTemplates)
            );

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.ModifiedCount > 0;
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
    
/**
         * 
         * Billing Plan Config
         * 
        **/
        public async Task<bool> AddUpdateBillingPlanConfig(BillingPlanConfig billingPlanConfig)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BillingPlanConfigField);
            var update = Builders<BsonDocument>.Update.Set(BillingPlanConfigField,
                BsonDocument.Create(billingPlanConfig)
            );

            var result = await _applicationConfigurationCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }

        public async Task<BillingPlanConfig?> GetBillingPlanConfig()
        {
            var filter = Builders<BsonDocument>.Filter.Eq("_id", BillingPlanConfigField);
            var result = await _applicationConfigurationCollection.Find(filter).FirstOrDefaultAsync();
            if (result == null) return null;
            return BsonSerializer.Deserialize<BillingPlanConfig>(result);
        }
    }
}

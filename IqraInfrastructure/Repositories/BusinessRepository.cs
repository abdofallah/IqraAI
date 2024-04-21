using IqraCore.Entities.Business;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class BusinessRepository : IBusinessRepository
    {
        private readonly IMongoCollection<Business> _businessCollection;

        public BusinessRepository(IMongoDatabase database)
        {
            _businessCollection = database.GetCollection<Business>("businesses");
        }

        public async Task<Business?> GetBusinessAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            return await _businessCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> AddBusinessAsync(Business business)
        {
            await _businessCollection.InsertOneAsync(business);
            return true;
        }

        public async Task<bool> DeleteBusinessAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var result = await _businessCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<bool> UpdateBusinessAsync(long businessId, UpdateDefinition<Business> updateDefinition)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var result = await _businessCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessPropertyAsync<T>(long businessId, string propertyName, T value)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var update = Builders<Business>.Update.Set($"Business.{propertyName}", value);
            var result = await _businessCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessAzureSettingsAsync(long businessId, BusinessAzureSettings azureSettings)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var update = Builders<Business>.Update.Set(b => b.BusinessAzureSettings, azureSettings);
            var result = await _businessCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBusinessPromptAsync(long businessId, Dictionary<string, string> businessPrompt, string promptType)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var update = Builders<Business>.Update.Set($"Business{promptType}", businessPrompt);
            var result = await _businessCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }    

        public async Task<bool> UpdateBusinessPhoneNumberAsync(long businessId, string phoneNumber)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var update = Builders<Business>.Update.Set(b => b.BusinessPhoneNumber, phoneNumber);
            var result = await _businessCollection.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<Dictionary<string, string>?> GetBusinessNameAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessName);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.BusinessName;
        }

        public async Task<Dictionary<string, string>?> GetBusinessSystemPromptAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessSystemPrompt);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.BusinessSystemPrompt;
        }

        public async Task<Dictionary<string, string>?> GetBusinessInitialMessageAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessInitialMessage);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.BusinessInitialMessage;
        }

        public async Task<List<string>?> GetBusinessLanguagesEnabledAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.LanguagesEnabled);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.LanguagesEnabled;
        }

        public async Task<BusinessAzureSettings?> GetBusinessAzureSettingsAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessAzureSettings);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.BusinessAzureSettings;
        }

        public async Task<string> GetBusinessPhoneNumberAsync(long businessId)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessId, businessId);
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessPhoneNumber);
            return (await _businessCollection.Find(filter).Project<Business>(projection).FirstOrDefaultAsync())?.BusinessPhoneNumber;
        }
    }
}
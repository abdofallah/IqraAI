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

        public async Task<List<Business>> GetBusinessesMetadataAsync()
        {
            var projection = Builders<Business>.Projection.Include(b => b.BusinessId).Include(b => b.BusinessName).Include(b => b.BusinessPhoneNumber);
            return await _businessCollection.Find(_ => true).Project<Business>(projection).ToListAsync();
        }

        public async Task<Business?> GetBusinessByPhoneNumberAsync(string phoneNumber)
        {
            var filter = Builders<Business>.Filter.Eq(b => b.BusinessPhoneNumber, phoneNumber);
            return await _businessCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<Business>> GetBusinessesAsync()
        {
            return await _businessCollection.Find(_ => true).ToListAsync();
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
    }
}
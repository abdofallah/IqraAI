using IqraCore.Entities.Helper.Business.Number;
using IqraCore.Entities.Number;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Number
{
    public class NumberRepository
    {
        private readonly string CollectionName = "Number";

        private readonly IMongoCollection<BusinessNumberData> _numberCollection;

        public NumberRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _numberCollection = database.GetCollection<BusinessNumberData>(CollectionName);
        }

        public async Task InsertNumberAsync(BusinessNumberData numberData)
        {
            await _numberCollection.InsertOneAsync(numberData);
        }

        public async Task<BusinessNumberData?> GetNumberAsync(string id)
        {
            return await _numberCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<BusinessNumberData>?> GetUserNumberByIdsAsync(List<string> numberIds, string userEmail)
        {
            var filter = Builders<BusinessNumberData>.Filter.In(x => x.Id, numberIds) & Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<List<BusinessNumberData>?> GetBusinessNumberByIdsAsync(List<string> numberIds, long businessId)
        {
            var filter = Builders<BusinessNumberData>.Filter.In(x => x.Id, numberIds) & Builders<BusinessNumberData>.Filter.Eq(x => x.AssignedToBusinessId, businessId);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<List<BusinessNumberData>?> GetNumbersAsync(int page, int pageSize)
        {
            return await _numberCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<BusinessNumberData>?> GetNumbersByProviderAsync(BusinessNumberProviderEnum provider, int page, int pageSize)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.Provider, provider);
            return await _numberCollection.Find(filter).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<BusinessNumberData>?> GetUserNumbersByProvider(BusinessNumberProviderEnum provider, string email, int page, int pageSize)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.Provider, provider) & Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, email);
            return await _numberCollection.Find(filter).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<BusinessNumberData>?> GetUserNumbers(string email)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, email);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<bool> CheckUserNumberExists(string exisitingNumberId, string userEmail)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.Id, exisitingNumberId) & Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).AnyAsync();
        }

        public async Task<BusinessNumberData?> GetUserNumberById(string exisitingNumberId, string userEmail)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.Id, exisitingNumberId) & Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> ReplaceNumberAsync(BusinessNumberData newNumberData)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.Id, newNumberData.Id);
            var result = await _numberCollection.ReplaceOneAsync(filter, newNumberData);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckUserNumberExistsByNumber(string numberCountryCode, string phoneNumber, string userEmail)
        {
            var filter = Builders<BusinessNumberData>.Filter.Eq(x => x.CountryCode, numberCountryCode) & Builders<BusinessNumberData>.Filter.Eq(x => x.Number, phoneNumber) & Builders<BusinessNumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).AnyAsync();
        }
    }
}

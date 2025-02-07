using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Number;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Number
{
    public class NumberRepository
    {
        private readonly string CollectionName = "Number";

        private readonly IMongoCollection<NumberData> _numberCollection;

        public NumberRepository(string connectionString, string databaseName)
        {
            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _numberCollection = database.GetCollection<NumberData>(CollectionName);
        }

        public async Task InsertNumberAsync(NumberData numberData)
        {
            await _numberCollection.InsertOneAsync(numberData);
        }

        public async Task<NumberData?> GetNumberAsync(string id)
        {
            return await _numberCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<NumberData>?> GetUserNumberByIdsAsync(List<string> numberIds, string userEmail)
        {
            var filter = Builders<NumberData>.Filter.In(x => x.Id, numberIds) & Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<List<NumberData>?> GetBusinessNumberByIdsAsync(List<string> numberIds, long businessId)
        {
            var filter = Builders<NumberData>.Filter.In(x => x.Id, numberIds) & Builders<NumberData>.Filter.Eq(x => x.AssignedToBusinessId, businessId);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<List<NumberData>?> GetNumbersAsync(int page, int pageSize)
        {
            return await _numberCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<NumberData>?> GetNumbersByProviderAsync(NumberProviderEnum provider, int page, int pageSize)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.Provider, provider);
            return await _numberCollection.Find(filter).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<NumberData>?> GetUserNumbersByProvider(NumberProviderEnum provider, string email, int page, int pageSize)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.Provider, provider) & Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, email);
            return await _numberCollection.Find(filter).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<List<NumberData>?> GetUserNumbers(string email)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, email);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<bool> CheckUserNumberExists(string exisitingNumberId, string userEmail)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.Id, exisitingNumberId) & Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).AnyAsync();
        }

        public async Task<NumberData?> GetUserNumberById(string exisitingNumberId, string userEmail)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.Id, exisitingNumberId) & Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> ReplaceNumberAsync(NumberData newNumberData)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.Id, newNumberData.Id);
            var result = await _numberCollection.ReplaceOneAsync(filter, newNumberData);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CheckUserNumberExistsByNumber(string numberCountryCode, string phoneNumber, string userEmail)
        {
            var filter = Builders<NumberData>.Filter.Eq(x => x.CountryCode, numberCountryCode) & Builders<NumberData>.Filter.Eq(x => x.Number, phoneNumber) & Builders<NumberData>.Filter.Eq(x => x.MasterUserEmail, userEmail);
            return await _numberCollection.Find(filter).AnyAsync();
        }
    }
}

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

        public async Task AddNumberAsync(NumberData numberData)
        {
            await _numberCollection.InsertOneAsync(numberData);
        }

        public async Task<NumberData?> GetNumberAsync(string id)
        {
            return await _numberCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<NumberData>?> GetNumberByIdsAsync(List<string> numberIds)
        {
            var filter = Builders<NumberData>.Filter.In(x => x.Id, numberIds);
            return await _numberCollection.Find(filter).ToListAsync();
        }

        public async Task<List<NumberData>?> GetNumbersAsync(int page, int pageSize)
        {
            return await _numberCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }
    }
}

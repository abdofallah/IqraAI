using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
namespace IqraInfrastructure.Repositories.Counter
{
    public class Counter
    {
        [BsonId]
        public string Id { get; set; }
        public long SequenceValue { get; set; }
    }

    public class CounterRepository
    {
        private readonly IMongoCollection<Counter> _countersCollection;

        public CounterRepository(IMongoClient client, string databaseName)
        {
            var database = client.GetDatabase(databaseName);
            _countersCollection = database.GetCollection<Counter>("Counters");
        }

        public async Task<long> GetNextSequenceValueAsync(string counterName)
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, counterName);
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);
            var options = new FindOneAndUpdateOptions<Counter, Counter>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            var counter = await _countersCollection.FindOneAndUpdateAsync(filter, update, options);
            return counter.SequenceValue;
        }
    }
}

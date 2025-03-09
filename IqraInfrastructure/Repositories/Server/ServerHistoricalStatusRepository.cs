using IqraCore.Entities.Server;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerHistoricalStatusRepository
    {
        private readonly string CollectionName = "-ServerHistoricalStatus";

        private readonly IMongoCollection<ServerHistoricalStatusData> _serverStatusCollection;

        public ServerHistoricalStatusRepository(string serverIdentifier, string connectionString, string databaseName)
        {
            CollectionName = serverIdentifier + CollectionName;

            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _serverStatusCollection = database.GetCollection<ServerHistoricalStatusData>(CollectionName);

            // Create DateTime Index
            _serverStatusCollection.Indexes.CreateOne(new CreateIndexModel<ServerHistoricalStatusData>(Builders<ServerHistoricalStatusData>.IndexKeys.Ascending(d => d.DateTime)));
        }

        public async Task InsertAsync(ServerHistoricalStatusData serverStatusHistoricalData)
        {
            await _serverStatusCollection.InsertOneAsync(serverStatusHistoricalData);
        }

        public async Task<List<ServerHistoricalStatusData>> GetBetweenDatetimeAsync(DateTime? start, DateTime? end)
        {
            if (start == null || end == null || start > end) return new List<ServerHistoricalStatusData>();

            var filter = Builders<ServerHistoricalStatusData>.Filter.Empty;

            if (start == end)
            {
                filter = Builders<ServerHistoricalStatusData>.Filter.Eq(d => d.DateTime, start);
            }

            if (start == null)
            {

                filter = Builders<ServerHistoricalStatusData>.Filter.Lte(d => d.DateTime, end);
            }

            if (end == null)
            {
                filter = Builders<ServerHistoricalStatusData>.Filter.Gte(d => d.DateTime, start);
            }

            return await _serverStatusCollection.Find(filter).ToListAsync();
        }
    }
}

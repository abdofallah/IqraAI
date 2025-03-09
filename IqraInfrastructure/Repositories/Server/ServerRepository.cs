using MongoDB.Bson;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerRepository
    {
        private readonly string CollectionName = "-Server";

        private readonly IMongoCollection<BsonDocument> _serversCollection;

        public ServerRepository(string serverIdentifier, string connectionString, string databaseName)
        {
            CollectionName = serverIdentifier + CollectionName;

            IMongoClient client = new MongoClient(connectionString);
            IMongoDatabase database = client.GetDatabase(databaseName);
            _serversCollection = database.GetCollection<BsonDocument>(CollectionName);
        }
    }
}

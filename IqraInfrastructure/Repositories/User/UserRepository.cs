using IqraCore.Entities.User;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories.User
{
    public class UserRepository
    {
        private readonly ILogger<UserRepository> _logger;

        private readonly string CollectionName = "Users";
        private readonly IMongoCollection<UserData> _usersCollection;

        public UserRepository(ILogger<UserRepository> logger, IMongoClient client, string databaseName)
        {
            _logger = logger;

            IMongoDatabase database = client.GetDatabase(databaseName);
            _usersCollection = database.GetCollection<UserData>(CollectionName);
        }

        public UserRepository(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<UserData>(CollectionName);
        }

        public async Task<bool> AddUserAsync(UserData user)
        {
            await _usersCollection.InsertOneAsync(user);
            return true;
        }

        public async Task<UserData?> GetUserByEmail(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUser(string email, UpdateDefinition<UserData> updateDefinition)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            return await UpdateUser(filter, updateDefinition);
        }

        public async Task<bool> UpdateUser(FilterDefinition<UserData> filter, UpdateDefinition<UserData> updateDefinition)
        {
            var result = await _usersCollection.UpdateOneAsync(filter, updateDefinition);
            return result.IsAcknowledged;
        }

        public async Task<bool> UpdateUser(FilterDefinition<UserData> filter, UpdateDefinition<UserData> updateDefinition, IClientSessionHandle session)
        {
            var result = await _usersCollection.UpdateOneAsync(session, filter, updateDefinition);
            return result.IsAcknowledged;
        }

        public Task<List<UserData>> GetUsersAsync()
        {
            return _usersCollection.Find(_ => true).ToListAsync();
        }

        public Task<List<UserData>> GetUsersAsync(int page, int pageSize)
        {
            return _usersCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }
    }
}

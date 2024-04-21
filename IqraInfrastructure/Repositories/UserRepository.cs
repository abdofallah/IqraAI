using IqraCore.Entities.User;
using IqraCore.Interfaces.Repositories;
using MongoDB.Driver;

namespace IqraInfrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _usersCollection;
        public UserRepository (IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
        }

        public async Task<bool> AddUserAsync(User user)
        {
            await _usersCollection.InsertOneAsync(user);
            return true;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            var filter = Builders<User>.Filter.Eq(b => b.Email, email);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUser(string email, UpdateDefinition<User> updateDefinition)
        {
            var filter = Builders<User>.Filter.Eq(b => b.Email, email);
            var result = await _usersCollection.UpdateOneAsync(filter, updateDefinition);
            return result.ModifiedCount > 0;
        }
    }
}

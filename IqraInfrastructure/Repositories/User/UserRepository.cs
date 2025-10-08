using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing;
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

        public async Task<bool> UpdateUser(string email, UpdateDefinition<UserData> updateDefinition, IClientSessionHandle session)
        {
            var filter = Builders<UserData>.Filter.Eq(b => b.Email, email);
            var result = await _usersCollection.UpdateOneAsync(session, filter, updateDefinition);
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

        public Task<List<UserData>> GetUsersAsync(FilterDefinition<UserData> filter)
        {
            return _usersCollection.Find(filter).ToListAsync();
        }

        public Task<List<UserData>> GetUsersAsync(int page, int pageSize)
        {
            return _usersCollection.Find(_ => true).Skip(page * pageSize).Limit(pageSize).ToListAsync();
        }

        public async Task<UserData?> GetUserByEmailHashAsync(string emailHash)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.EmailHash, emailHash);
            return await _usersCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> CheckUserExistsByEmail(string email)
        {
            var filter = Builders<UserData>.Filter.Eq(u => u.Email, email);
            return await _usersCollection.Find(filter).AnyAsync();
        }

        public async Task<bool> TryIncrementConcurrencyUsageAsync(string userEmail, string featureKey, long maxConcurrency, UserBillingCycleConcurrencyFeatureUsage usageItem)
        {
            string arrayFieldPath = $"Billing.CurrentCycleUsage.CurrentConcurrencyFeatureUsage.{featureKey}";

            var filterBuilder = Builders<UserData>.Filter;

            var userFilter = filterBuilder.Eq(u => u.Email, userEmail);

            var concurrencyLimitFilter = filterBuilder.Not(filterBuilder.Exists($"{arrayFieldPath}.{maxConcurrency - 1}"));

            var finalFilter = filterBuilder.And(userFilter, concurrencyLimitFilter);
            var update = Builders<UserData>.Update.Push(arrayFieldPath, usageItem);
            var result = await _usersCollection.UpdateOneAsync(finalFilter, update);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> DecrementConcurrencyUsageAsync(string userEmail, string featureKey, long businessId, object parentReference, object? childReference)
        {
            string arrayFieldPath = $"Billing.CurrentCycleUsage.CurrentConcurrencyFeatureUsage.{featureKey}";

            var userFilter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail);

            var update = Builders<UserData>.Update.PullFilter(
                arrayFieldPath,
                Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.And(
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.BusinessId, businessId),
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.ParentReference, parentReference),
                    Builders<UserBillingCycleConcurrencyFeatureUsage>.Filter.Eq(i => i.ChildReference, childReference)
                )
            );

            var result = await _usersCollection.UpdateOneAsync(userFilter, update);
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }
    }
}

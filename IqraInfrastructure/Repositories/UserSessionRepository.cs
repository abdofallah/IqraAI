using StackExchange.Redis;

namespace IqraInfrastructure.Repositories
{
    public class UserSessionRepository
    {
        private readonly ConnectionMultiplexer _redis;
        public UserSessionRepository(string connectionString)
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
        }

        public async Task<bool> CreateSession(string userId, string sessionId, string authenticationKey, int expireHours)
        {
            var db = _redis.GetDatabase();
            TimeSpan expiration = TimeSpan.FromHours(expireHours);

            return await db.StringSetAsync($"{userId}:{sessionId}", authenticationKey, expiration);
        }

        public async Task<bool> RemoveSession(string userId, string sessionId)
        {
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync($"{userId}:{sessionId}");
        }

        public async Task<string?> RetrieveSession(string userId, string sessionId)
        {
            var db = _redis.GetDatabase();
            return await db.StringGetAsync($"{userId}:{sessionId}");
        }

        public async Task<bool> ValidateSession(string userId, string sessionId, string authenticationKey)
        {
            var db = _redis.GetDatabase();
            string? storedAuthKey = await db.StringGetAsync($"{userId}:{sessionId}");

            return storedAuthKey == authenticationKey;
        }
    }
}

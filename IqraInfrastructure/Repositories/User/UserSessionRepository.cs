using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.User
{
    public class UserSessionRepository
    {
        private readonly ILogger<UserSessionRepository> _logger;

        private readonly RedisConnectionFactory _redisFactory;     

        public UserSessionRepository(ILogger<UserSessionRepository> logger, RedisConnectionFactory redisFactory)
        {
            _logger = logger;
            _redisFactory = redisFactory;
        }

        public async Task<bool> CreateSession(string userId, string sessionId, string authenticationKey, int expireHours)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                TimeSpan expiration = TimeSpan.FromHours(expireHours);

                return await db.StringSetAsync($"{userId}:{sessionId}", authenticationKey, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session");
                return false;
            }
        }

        public async Task<bool> RemoveSession(string userId, string sessionId)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                return await db.KeyDeleteAsync($"{userId}:{sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing session");
                return false;
            }
        }

        public async Task<string?> RetrieveSession(string userId, string sessionId)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                return await db.StringGetAsync($"{userId}:{sessionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session");
                return null;
            }
        }

        public async Task<bool> ValidateSession(string userId, string sessionId, string authenticationKey)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                string? storedAuthKey = await db.StringGetAsync($"{userId}:{sessionId}");

                return storedAuthKey == authenticationKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session");
                return false;
            }
        }
    }
}

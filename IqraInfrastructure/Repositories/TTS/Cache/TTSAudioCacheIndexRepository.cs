using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IqraInfrastructure.Repositories.TTS.Cache
{
    public class TTSAudioCacheIndexRepository
    {
        public static int DATABASE_INDEX = 3;

        private readonly ILogger<TTSAudioCacheIndexRepository> _logger;
        private readonly RedisConnectionFactory _redisFactory; // Using your existing factory
        private const string RedisKeyPrefix = "tts-cache:";

        public TTSAudioCacheIndexRepository(ILogger<TTSAudioCacheIndexRepository> logger, RedisConnectionFactory redisFactory)
        {
            _logger = logger;
            _redisFactory = redisFactory;
        }

        private IDatabase GetDatabase()
        {
            // This should return the region-local Redis instance.
            return _redisFactory.GetDatabase();
        }

        public async Task<(bool success, string? value)> GetAsync(string cacheKey)
        {
            try
            {
                var db = GetDatabase();
                var value = await db.StringGetAsync(RedisKeyPrefix + cacheKey);
                return (true, value.HasValue ? (string)value : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from Redis for key {CacheKey}", cacheKey);
                return (false, null); // On failure, treat it as a cache miss.
            }
        }

        public async Task<bool> SetAsync(string cacheKey, string value, TimeSpan expiry)
        {
            try
            {
                var db = GetDatabase();
                return await db.StringSetAsync(RedisKeyPrefix + cacheKey, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting Redis key {CacheKey}", cacheKey);
                return false;
            }
        }
    }
}

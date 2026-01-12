using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IqraInfrastructure.Managers.KnowledgeBase
{
    public class KnowledgeBaseCollectionsLoadManager
    {
        public static int DATABASE_INDEX = 10;

        private readonly ILogger<KnowledgeBaseCollectionsLoadManager> _logger;
        private readonly MilvusKnowledgeBaseClient _milvusClient;
        private readonly RedisConnectionFactory _redisFactory;
        private readonly string _databaseName;

        private const string SessionsKeyPrefix = "milvus:collection:{0}:sessions";

        public KnowledgeBaseCollectionsLoadManager(ILogger<KnowledgeBaseCollectionsLoadManager> logger, MilvusKnowledgeBaseClient milvusClient, string databaseName, RedisConnectionFactory redisFactory)
        {
            _logger = logger;
            _milvusClient = milvusClient;
            _redisFactory = redisFactory;
            _databaseName = databaseName;
        }

        public async Task<bool> RegisterUseAsync(string collectionName, string sessionId, TimeSpan expiry)
        {
            var db = _redisFactory.GetDatabase();
            var sessionsKey = string.Format(SessionsKeyPrefix, collectionName);
            var expiryTimestamp = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();

            try
            {
                var redisKey = new RedisKey(sessionsKey);
                var redisValue = new RedisValue(sessionId);

                if ((await db.SortedSetScoreAsync(redisKey, redisValue)) != null)
                {
                    bool removeSession = await db.SortedSetRemoveAsync(redisKey, redisValue);
                    if (!removeSession)
                    {
                        return false;
                    }
                }

                bool addCollectionAndSession = await db.SortedSetAddAsync(redisKey, redisValue, expiryTimestamp);
                if (!addCollectionAndSession)
                {
                    return false;
                }

                bool loaded = await _milvusClient.LoadCollectionAsync(_databaseName, collectionName);
                if (!loaded)
                {
                    _logger.LogError("CRITICAL: Failed to load collection {CollectionName}. Rolling back registration.", collectionName);
                    await DeregisterUseAsync(collectionName, sessionId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during RegisterUseAsync for {CollectionName}.", collectionName);
                return false;
            }
        }

        public async Task<bool> DeregisterUseAsync(string collectionName, string sessionId)
        {
            var db = _redisFactory.GetDatabase();
            var sessionsKey = string.Format(SessionsKeyPrefix, collectionName);

            if (!await db.KeyExistsAsync(sessionsKey))
            {
                return true;
            }

            var redisKey = new RedisKey(sessionsKey);
            var redisValue = new RedisValue(sessionId);
            await db.SortedSetUpdateAsync(redisKey, redisValue, 0);
            return true;
        }
    }
}

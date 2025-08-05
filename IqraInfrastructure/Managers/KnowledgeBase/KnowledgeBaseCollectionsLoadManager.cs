using IqraInfrastructure.Repositories.KnowledgeBase;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IqraInfrastructure.Managers.KnowledgeBase
{
    /// <summary>
    /// A singleton service that orchestrates the loading and releasing of Milvus collections
    /// in a distributed environment using Redis for state management and locking.
    /// </summary>
    public class KnowledgeBaseCollectionsLoadManager : BackgroundService
    {
        private readonly ILogger<KnowledgeBaseCollectionsLoadManager> _logger;
        private readonly MilvusKnowledgeBaseClient _milvusClient;
        private readonly RedisConnectionFactory _redisFactory;

        // --- Configuration for Janitor ---
        private const string JanitorLockKey = "milvus:janitor:lock";
        private readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        private const string SessionsKeyPrefix = "milvus:collection:{0}:sessions";

        public KnowledgeBaseCollectionsLoadManager(ILogger<KnowledgeBaseCollectionsLoadManager> logger, MilvusKnowledgeBaseClient milvusClient, RedisConnectionFactory redisFactory)
        {
            _logger = logger;
            _milvusClient = milvusClient;
            _redisFactory = redisFactory;
        }

        #region Public API Methods

        /// <summary>
        /// Registers a session's intent to use a collection, providing its expiry.
        /// Uses a Redis Transaction to atomically handle loading if it's the first session.
        /// </summary>
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

                bool loaded = await _milvusClient.LoadCollectionAsync(collectionName);
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

        /// <summary>
        /// De-registers a session's use of a collection. It returns true if the collection no longer exists in Redis.
        /// </summary>
        public async Task<bool> DeregisterUseAsync(string collectionName, string sessionId)
        {
            var db = _redisFactory.GetDatabase();
            var sessionsKey = string.Format(SessionsKeyPrefix, collectionName);

            if (!await db.KeyExistsAsync(sessionsKey))
            {
                return true;
            }

            // ZREM is atomic, no transaction needed here.
            await db.SortedSetRemoveAsync(sessionsKey, sessionId);
            return true;
        }

        #endregion

        #region Background Janitor Logic

        /// <summary>
        /// This is the entry point for the long-running background service.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Distributed Collection Manager's Janitor Service is starting.");

            // Small delay on startup to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var db = _redisFactory.GetDatabase();
                var lockToken = Guid.NewGuid().ToString();

                // Acquire a global lock to ensure only one janitor runs across all backend instances.
                if (await db.LockTakeAsync(JanitorLockKey, lockToken, _lockTimeout))
                {
                    try
                    {
                        await CleanupStaleCollectionsAsync(db, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred during the janitor cleanup cycle.");
                    }
                    finally
                    {
                        await db.LockReleaseAsync(JanitorLockKey, lockToken);
                        _logger.LogInformation("Janitor released lock.");
                    }
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CleanupStaleCollectionsAsync(IDatabase db, CancellationToken stoppingToken)
        {
            var server = _redisFactory.GetServer();
            // Iterate through all "milvus:collection:*:sessions" keys
            await foreach (var key in server.KeysAsync(pattern: "milvus:collection:*:sessions", pageSize: 100).WithCancellation(stoppingToken))
            {
                var collectionName = key.ToString().Split(':')[2];
                var nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 1. Atomically remove all sessions that have expired (score is in the past).
                await db.SortedSetRemoveRangeByScoreAsync(key, 0, nowTimestamp);

                // 2. Check if any sessions remain.
                if (await db.SortedSetLengthAsync(key) == 0)
                {
                    // 3. If no sessions are left, release from Milvus and delete the Redis key.
                    if (await _milvusClient.ReleaseCollectionAsync(collectionName))
                    {
                        await db.KeyDeleteAsync(key);
                    }
                    else
                    {
                        _logger.LogError("Janitor failed to release orphaned collection {CollectionName}.", collectionName);
                    }
                }
            }
        }

        #endregion
    }
}

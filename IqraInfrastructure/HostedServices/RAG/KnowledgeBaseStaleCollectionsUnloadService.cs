using IqraInfrastructure.Repositories.KnowledgeBase.Vector;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using StackExchange.Redis;

namespace IqraInfrastructure.HostedServices.RAG
{
    public class KnowledgeBaseStaleCollectionsUnloadService : BackgroundService
    {
        private readonly ILogger<KnowledgeBaseStaleCollectionsUnloadService> _logger;
        private readonly MilvusKnowledgeBaseClient _milvusClient;
        private readonly RedisConnectionFactory _redisFactory;
        private readonly string _databaseName;

        private const string JanitorLockKey = "milvus:janitor:lock";
        private readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public KnowledgeBaseStaleCollectionsUnloadService(
            ILogger<KnowledgeBaseStaleCollectionsUnloadService> logger,
            MilvusKnowledgeBaseClient milvusClient,
            string databaseName,
            RedisConnectionFactory redisFactory
        ) {
            _logger = logger;
            _milvusClient = milvusClient;
            _redisFactory = redisFactory;
            _databaseName = databaseName;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Distributed Collection Manager's Janitor Service is starting.");

            // Small delay on startup to allow the application to fully initialize
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var db = _redisFactory.GetDatabase();
                var lockToken = ObjectId.GenerateNewId().ToString();

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
                    if (await _milvusClient.ReleaseCollectionAsync(_databaseName, collectionName))
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
    }
}

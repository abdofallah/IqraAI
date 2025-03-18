using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IqraInfrastructure.Repositories.Server
{
    public class DistributedLockRepository
    {
        private readonly RedisConnectionFactory _redisFactory;
        private readonly ILogger<DistributedLockRepository> _logger;

        private bool _lockAcquired = false;
        private Timer? _renewalTimer;

        public DistributedLockRepository(
            RedisConnectionFactory redisFactory,
            ILogger<DistributedLockRepository> logger)
        {
            _redisFactory = redisFactory;
            _logger = logger;
        }

        public async Task<bool> AcquireAsync(string lockKey, string lockValue, TimeSpan expiry, TimeSpan? timeout = null)
        {
            if (_lockAcquired)
                return true;

            timeout ??= TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            var db = _redisFactory.GetDatabase();

            while (DateTime.UtcNow - startTime < timeout)
            {
                _lockAcquired = await db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);

                if (_lockAcquired)
                {
                    _logger.LogDebug("Lock acquired: {LockKey}", lockKey);

                    // Start renewal timer at 1/2 of the expiry time
                    var renewalInterval = TimeSpan.FromMilliseconds(expiry.TotalMilliseconds / 2);
                    _renewalTimer = new Timer(RenewLock, new Dictionary<string, object>() { { "lockKey", lockKey }, { "lockValue", lockValue }, { "expiry", expiry } }, renewalInterval, renewalInterval);

                    return true;
                }

                await Task.Delay(100);
            }

            _logger.LogWarning("Failed to acquire lock: {LockKey}", lockKey);
            return false;
        }

        public async Task ReleaseAsync(string lockKey, string lockValue)
        {
            if (!_lockAcquired)
                return;

            _renewalTimer?.Dispose();
            _renewalTimer = null;

            var db = _redisFactory.GetDatabase();

            // Only release if the lock is still owned by us
            var script = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { lockKey },
                new RedisValue[] { lockValue }
            );

            _lockAcquired = false;
            _logger.LogDebug("Lock released: {LockKey}", lockKey);
        }

        private async void RenewLock(object? state)
        {
            if (!_lockAcquired)
                return;

            if (state == null)
            {
                _logger.LogError("Renew lock state is null");
                return;
            }

            Dictionary<string, object> stateDict = (Dictionary<string, object>)state;

            string lockKey = (string)stateDict["lockKey"];
            string lockValue = (string)stateDict["lockValue"];
            TimeSpan expiry = (TimeSpan)stateDict["expiry"];

            try
            {
                var db = _redisFactory.GetDatabase();

                // Only renew if the lock is still owned by us
                var script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('pexpire', KEYS[1], ARGV[2])
                    else
                        return 0
                    end";

                var result = await db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { lockKey },
                    new RedisValue[] { lockValue, (int)expiry.TotalMilliseconds }
                );

                if ((int)result == 1)
                {
                    _logger.LogDebug("Lock renewed: {LockKey}", lockKey);
                }
                else
                {
                    _logger.LogWarning("Failed to renew lock: {LockKey}", lockKey);
                    _lockAcquired = false;
                    _renewalTimer?.Dispose();
                    _renewalTimer = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing lock: {LockKey}", lockKey);
            }
        }
    }
}
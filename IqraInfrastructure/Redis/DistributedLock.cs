using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IqraInfrastructure.Redis
{
    public class DistributedLock : IDisposable
    {
        private readonly IRedisConnectionFactory _redisFactory;
        private readonly ILogger<DistributedLock> _logger;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private readonly TimeSpan _expiry;
        private bool _lockAcquired = false;
        private Timer? _renewalTimer;

        public DistributedLock(
            string lockKey,
            TimeSpan expiry,
            IRedisConnectionFactory redisFactory,
            ILogger<DistributedLock> logger)
        {
            _lockKey = $"lock:{lockKey}";
            _lockValue = Guid.NewGuid().ToString();
            _expiry = expiry;
            _redisFactory = redisFactory;
            _logger = logger;
        }

        public async Task<bool> AcquireAsync(TimeSpan? timeout = null)
        {
            if (_lockAcquired)
                return true;

            timeout ??= TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            var db = _redisFactory.GetDatabase();

            while (DateTime.UtcNow - startTime < timeout)
            {
                _lockAcquired = await db.StringSetAsync(_lockKey, _lockValue, _expiry, When.NotExists);

                if (_lockAcquired)
                {
                    _logger.LogDebug("Lock acquired: {LockKey}", _lockKey);

                    // Start renewal timer at 1/2 of the expiry time
                    var renewalInterval = TimeSpan.FromMilliseconds(_expiry.TotalMilliseconds / 2);
                    _renewalTimer = new Timer(RenewLock, null, renewalInterval, renewalInterval);

                    return true;
                }

                await Task.Delay(100);
            }

            _logger.LogWarning("Failed to acquire lock: {LockKey}", _lockKey);
            return false;
        }

        public async Task ReleaseAsync()
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
                new RedisKey[] { _lockKey },
                new RedisValue[] { _lockValue }
            );

            _lockAcquired = false;
            _logger.LogDebug("Lock released: {LockKey}", _lockKey);
        }

        private async void RenewLock(object? state)
        {
            if (!_lockAcquired)
                return;

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
                    new RedisKey[] { _lockKey },
                    new RedisValue[] { _lockValue, (int)_expiry.TotalMilliseconds }
                );

                if ((int)result == 1)
                {
                    _logger.LogDebug("Lock renewed: {LockKey}", _lockKey);
                }
                else
                {
                    _logger.LogWarning("Failed to renew lock: {LockKey}", _lockKey);
                    _lockAcquired = false;
                    _renewalTimer?.Dispose();
                    _renewalTimer = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing lock: {LockKey}", _lockKey);
            }
        }

        public void Dispose()
        {
            _renewalTimer?.Dispose();
            _renewalTimer = null;

            if (_lockAcquired)
            {
                ReleaseAsync().GetAwaiter().GetResult();
            }
        }
    }
}
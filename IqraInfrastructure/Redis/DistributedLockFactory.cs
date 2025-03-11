using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Redis
{
    public class DistributedLockFactory : IDistributedLockFactory
    {
        private readonly IRedisConnectionFactory _redisFactory;
        private readonly ILoggerFactory _loggerFactory;

        public DistributedLockFactory(
            IRedisConnectionFactory redisFactory,
            ILoggerFactory loggerFactory)
        {
            _redisFactory = redisFactory;
            _loggerFactory = loggerFactory;
        }

        public Task<DistributedLock> CreateLockAsync(string lockKey, TimeSpan expiry)
        {
            var logger = _loggerFactory.CreateLogger<DistributedLock>();
            var distributedLock = new DistributedLock(lockKey, expiry, _redisFactory, logger);
            return Task.FromResult(distributedLock);
        }
    }
}
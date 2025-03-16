using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.Server
{
    public class DistributedLockFactory
    {
        private readonly RedisConnectionFactory _redisFactory;
        private readonly ILoggerFactory _loggerFactory;

        public DistributedLockFactory(
            RedisConnectionFactory redisFactory,
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
namespace IqraInfrastructure.Redis
{
    public interface IDistributedLockFactory
    {
        Task<DistributedLock> CreateLockAsync(string lockKey, TimeSpan expiry);
    }
}

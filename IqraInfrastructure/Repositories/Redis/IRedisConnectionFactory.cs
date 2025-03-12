using StackExchange.Redis;

namespace IqraInfrastructure.Repositories.Redis
{
    public interface IRedisConnectionFactory
    {
        ConnectionMultiplexer GetConnection();
        IDatabase GetDatabase(int db = -1);
    }
}

using StackExchange.Redis;

namespace IqraInfrastructure.Redis
{
    public interface IRedisConnectionFactory
    {
        ConnectionMultiplexer GetConnection();
        IDatabase GetDatabase(int db = -1);
    }
}

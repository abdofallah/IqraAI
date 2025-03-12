using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.Redis
{
    public class RedisConnectionFactory : IRedisConnectionFactory
    {
        private readonly object _lock = new object();
        private readonly string _connectionString;
        private readonly ILogger<RedisConnectionFactory> _logger;
        private ConnectionMultiplexer _connection;
        private bool _isConnected = false;

        public RedisConnectionFactory(string connectionString, ILogger<RedisConnectionFactory> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public ConnectionMultiplexer GetConnection()
        {
            if (_isConnected && _connection?.IsConnected == true)
                return _connection;

            lock (_lock)
            {
                if (_isConnected && _connection?.IsConnected == true)
                    return _connection;

                if (_connection != null)
                {
                    try
                    {
                        _connection.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing Redis connection");
                    }

                    _connection = null;
                    _isConnected = false;
                }

                try
                {
                    var options = ConfigurationOptions.Parse(_connectionString);
                    options.AbortOnConnectFail = false;

                    _connection = ConnectionMultiplexer.Connect(options);
                    _connection.ConnectionFailed += OnConnectionFailed;
                    _connection.ConnectionRestored += OnConnectionRestored;

                    _isConnected = true;
                    _logger.LogInformation("Redis connection established");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error establishing Redis connection");
                    _isConnected = false;
                }

                return _connection;
            }
        }

        public IDatabase GetDatabase(int db = -1)
        {
            return GetConnection().GetDatabase(db);
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs args)
        {
            _logger.LogError("Redis connection failed: {EndPoint}, {FailureType}", args.EndPoint, args.FailureType);
            _isConnected = false;
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs args)
        {
            _logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
            _isConnected = true;
        }
    }
}

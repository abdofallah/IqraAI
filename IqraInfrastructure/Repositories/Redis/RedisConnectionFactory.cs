using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Repositories.Redis
{
    public class RedisConnectionFactory
    {
        private readonly object _lock = new object();
        private readonly string _connectionString;
        private readonly ILogger<RedisConnectionFactory> _logger;
        private ConnectionMultiplexer _connection;
        private bool _isConnected = false;
        private int _defaultDatabase = -1;

        public RedisConnectionFactory(string connectionString, ILogger<RedisConnectionFactory> logger)
        {
            _connectionString = connectionString;
            _logger = logger;

            // Init connection
            GetConnection();

            // Initial Test
            GetServer();
            GetDatabase();
        }

        private ConnectionMultiplexer GetConnection()
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
                    options.AsyncTimeout = 500;
                    options.ConnectTimeout = 500;
                    options.SyncTimeout = 500;
                    options.AbortOnConnectFail = false;

                    if (options.DefaultDatabase != null)
                    {
                        _defaultDatabase = options.DefaultDatabase.Value;
                    }

                    _connection = ConnectionMultiplexer.Connect(options);
                    _connection.ConnectionFailed += OnConnectionFailed;
                    _connection.ConnectionRestored += OnConnectionRestored;

                    _isConnected = true;
                    _logger.LogInformation("Redis connection established");
                }
                catch (Exception ex)
                {
                    throw new Exception("Error establishing Redis connection", ex);
                }

                return _connection;
            }
        }

        public IServer GetServer()
        {
            var connection = GetConnection();
            if (connection == null) return null;

            var endpoint = connection.GetEndPoints().FirstOrDefault();
            if (endpoint == null)
            {
                throw new Exception("No endpoints found for Redis connection.");
            }

            return connection.GetServer(endpoint);
        }

        public IDatabase GetDatabase()
        {
            return GetConnection().GetDatabase(_defaultDatabase);
        }

        public ConnectionMultiplexer GetConnectionMultiplexer()
        {
            return _connection;
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

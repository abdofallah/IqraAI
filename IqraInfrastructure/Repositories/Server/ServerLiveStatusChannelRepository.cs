using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerLiveStatusChannelRepository
    {
        private readonly IRedisConnectionFactory _redisFactory;
        private readonly ILogger<ServerLiveStatusChannelRepository> _logger;

        private readonly string _channelName = "server:status:updates";
        private readonly TimeSpan _statusExpiry = TimeSpan.FromMinutes(1);

        public ServerLiveStatusChannelRepository(IRedisConnectionFactory redisFactory, ILogger<ServerLiveStatusChannelRepository> logger)
        {
            _redisFactory = redisFactory;
            _logger = logger;
        }

        public async Task PublishServerStatusAsync(ServerStatusData status)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var json = JsonSerializer.Serialize(status);

                // Store the status with expiration
                await db.StringSetAsync($"server:status:{status.ServerId}", json, _statusExpiry);

                // Publish update notification
                await db.PublishAsync(_channelName, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing server status for {ServerId}", status.ServerId);
            }
        }

        public async Task<ServerStatusData?> GetServerStatusAsync(string serverId)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var json = await db.StringGetAsync($"server:status:{serverId}");

                if (json.IsNullOrEmpty)
                    return null;

                return JsonSerializer.Deserialize<ServerStatusData>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting server status for {ServerId}", serverId);
                return null;
            }
        }

        public async Task<List<ServerStatusData>> GetAllServerStatusesAsync()
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var keys = db.Multiplexer.GetServer(db.Multiplexer.GetEndPoints().First())
                    .Keys(pattern: "server:status:*");

                var results = new List<ServerStatusData>();

                foreach (var key in keys)
                {
                    var json = await db.StringGetAsync(key);
                    if (!json.IsNullOrEmpty)
                    {
                        var status = JsonSerializer.Deserialize<ServerStatusData>(json);
                        if (status != null)
                            results.Add(status);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all server statuses");
                return new List<ServerStatusData>();
            }
        }

        public async Task<List<ServerStatusData>> GetRegionServerStatusesAsync(string regionId)
        {
            var allStatuses = await GetAllServerStatusesAsync();
            return allStatuses.Where(s => s.RegionId == regionId).ToList();
        }
    }
}
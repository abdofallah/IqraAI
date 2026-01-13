using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraInfrastructure.Repositories.Redis;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Repositories.Server
{
    public class ServerLiveStatusChannelRepository
    {
        public static int DATABASE_INDEX = 7;

        private readonly RedisConnectionFactory _redisFactory;
        private readonly ILogger<ServerLiveStatusChannelRepository> _logger;
        private readonly TimeSpan _statusExpiry = TimeSpan.FromSeconds(30);

        public ServerLiveStatusChannelRepository(RedisConnectionFactory redisFactory, ILogger<ServerLiveStatusChannelRepository> logger)
        {
            _redisFactory = redisFactory;
            _logger = logger;
        }

        public async Task PublishServerStatusAsync(ServerStatusData status)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var json = "";
                var regionId = "singleton";
                if (status is BackendServerStatusData backendStatus)
                {
                    json = JsonSerializer.Serialize<BackendServerStatusData>(backendStatus);
                    regionId = backendStatus.RegionId;
                }
                else if (status is ProxyServerStatusData proxyStatus)
                {
                    json = JsonSerializer.Serialize<ProxyServerStatusData>(proxyStatus);
                    regionId = proxyStatus.RegionId;
                }
                else
                {
                    json = JsonSerializer.Serialize(status);
                }

                var key = $"server:status:{regionId}:{status.NodeId}";

                // Update status
                await db.StringSetAsync(key, json, _statusExpiry);
                // Publish update notification
                await db.PublishAsync($"{key}:updates", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing server status for {ServerId}", status.NodeId);
            }
        }

        public async Task RemoveServerStatusAsync(string regionId, string nodeId)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var key = $"server:status:{regionId}:{nodeId}";
                await db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing server status for {NodeId}", nodeId);
            }
        }

        public async Task<ServerStatusData?> GetServerStatusAsync(string regionId, string serverId)
        {
            try
            {
                var db = _redisFactory.GetDatabase();
                var json = await db.StringGetAsync($"server:status:{regionId}:{serverId}");

                if (json.IsNullOrEmpty)
                    return null;

                var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(json.ToString());
                if (jsonDoc == null)
                {
                    _logger.LogError("Invalid server status for region {RegionId} and server {ServerId}", regionId, serverId);
                    return null;
                }

                if (!jsonDoc.RootElement.TryGetProperty("Type", out var type) && type.ValueKind != JsonValueKind.Number)
                {
                    _logger.LogError("Invalid server status type value kind for region {RegionId} and server {ServerId}", regionId, serverId);
                    return null;
                }

                AppNodeTypeEnum nodeType = (AppNodeTypeEnum)type.GetInt32();
                if (nodeType == AppNodeTypeEnum.Backend)
                {
                    return JsonSerializer.Deserialize<BackendServerStatusData>(json.ToString());
                }
                else if (nodeType == AppNodeTypeEnum.Proxy)
                {
                    return JsonSerializer.Deserialize<ProxyServerStatusData>(json.ToString());
                }
                else if (nodeType != AppNodeTypeEnum.Unknown)
                {
                    return JsonSerializer.Deserialize<ServerStatusData>(json.ToString());
                }
                else
                {
                    _logger.LogError("Invalid server status type (unknown) for region {RegionId} and server {ServerId}", regionId, serverId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting server status for region {RegionId} and server {ServerId}", regionId, serverId);
                return null;
            }
        }

        public async Task<List<ServerStatusData>> GetAllActiveNodesAsync()
        {
            var results = new List<ServerStatusData>();
            var db = _redisFactory.GetDatabase();

            try
            {
                // We need the IServer interface to scan keys
                var muxer = _redisFactory.GetConnectionMultiplexer();
                var endPoint = muxer.GetEndPoints().FirstOrDefault();

                if (endPoint == null) return results;

                var server = muxer.GetServer(endPoint);

                // Scan for keys in DB 7 matching the pattern
                var keys = server.Keys(database: DATABASE_INDEX, pattern: "server:status:*");

                var keyList = keys.ToArray();
                if (keyList.Length == 0) return results;

                // Fetch all values in one RTT (Round Trip Time)
                var redisValues = await db.StringGetAsync(keyList);

                foreach (var val in redisValues)
                {
                    if (val.IsNullOrEmpty) continue;

                    try
                    {
                        var jsonDoc = JsonSerializer.Deserialize<JsonDocument>(val.ToString());
                        if (jsonDoc == null) continue;

                        if (jsonDoc.RootElement.TryGetProperty("Type", out var typeProp))
                        {
                            var type = (AppNodeTypeEnum)typeProp.GetInt32();

                            ServerStatusData? data = null;
                            if (type == AppNodeTypeEnum.Backend)
                                data = JsonSerializer.Deserialize<BackendServerStatusData>(val.ToString());
                            else if (type == AppNodeTypeEnum.Proxy)
                                data = JsonSerializer.Deserialize<ProxyServerStatusData>(val.ToString());
                            else
                                data = JsonSerializer.Deserialize<ServerStatusData>(val.ToString());

                            if (data != null) results.Add(data);
                        }
                    }
                    catch { /* Ignore malformed data */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning active nodes.");
            }

            return results;
        }
    }
}
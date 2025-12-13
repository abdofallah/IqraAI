using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Server;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server
{
    public class ServerSelectionManager
    {
        private readonly ILogger<ServerSelectionManager> _logger;
        private readonly RegionManager _regionManager;
        private readonly ServerLiveStatusChannelRepository _serverStatusChannel;
        private readonly DistributedLockRepository _serverLock;

        public ServerSelectionManager(
            ILogger<ServerSelectionManager> logger,
            RegionManager regionManager,
            ServerLiveStatusChannelRepository serverStatusChannel,
            DistributedLockRepository lockFactory
        )
        {
            _logger = logger;
            _regionManager = regionManager;
            _serverStatusChannel = serverStatusChannel;
            _serverLock = lockFactory;
        }

        public async Task<FunctionReturnResult<List<ServerSelectionResultModel>?>> SelectOptimalServerAsync(string regionId, List<string>? ServersToIgnore = null)
        {
            var result = new FunctionReturnResult<List<ServerSelectionResultModel>?>();

            try
            {
                // Get the region data
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null || regionData.DisabledAt != null)
                {
                    result.Code = "SelectOptimalServerAsync:1";
                    result.Message = $"Region not available: {regionId}";
                    _logger.LogWarning("Region not available: {RegionId}", regionId);
                    return result;
                }

                // Get backend servers for the specified region
                var backendServers = regionData.Servers
                    .Where(s => s.Type == ServerTypeEnum.Backend && s.DisabledAt == null && (!ServersToIgnore?.Contains(s.Id) ?? true))
                    .Select(s => s.Id)
                    .ToList();

                if (!backendServers.Any())
                {
                    result.Code = "SelectOptimalServerAsync:2";
                    result.Message = $"No active backend servers in region: {regionId}";
                    _logger.LogWarning("No active backend servers in region: {RegionId}", regionId);
                    return result;
                }

                // Get current status for all servers
                var serverStatuses = new List<ServerStatusData>();
                foreach (var serverId in backendServers)
                {
                    var status = await _serverStatusChannel.GetServerStatusAsync(serverId);
                    if (status != null)
                    {
                        serverStatuses.Add(status);
                    }
                }

                if (!serverStatuses.Any())
                {
                    result.Code = "SelectOptimalServerAsync:3";
                    result.Message = "No server status data available";
                    _logger.LogWarning("No server status data available for region: {RegionId}", regionId);
                    return result;
                }

                // Filter out servers in maintenance mode
                serverStatuses = serverStatuses.Where(s => !s.MaintenanceMode).ToList();
                if (!serverStatuses.Any())
                {
                    result.Code = "SelectOptimalServerAsync:4";
                    result.Message = "All servers are in maintenance mode";
                    _logger.LogWarning("All servers in region {RegionId} are in maintenance mode", regionId);
                    return result;
                }

                // Apply scoring algorithm and select the best server
                var scoredServers = serverStatuses
                    .Select(s => (Server: s, Score: CalculateServerScore(s)))
                    .Where(s => s.Score > 0)
                    .OrderByDescending(s => s.Score)
                    .ToList();

                if (!scoredServers.Any())
                {
                    result.Code = "SelectOptimalServerAsync:5";
                    result.Message = "All servers are at capacity";
                    _logger.LogWarning("All servers in region {RegionId} are at capacity", regionId);
                    return result;
                }

                // Select the top three servers with the highest score
                var topServers = scoredServers
                    .Take(3)
                    .Select(
                        s =>
                        {
                            var serverData = regionData.Servers.First(b => b.Id == s.Server.ServerId);

                            return new ServerSelectionResultModel()
                            {
                                ServerId = s.Server.ServerId,
                                Score = s.Score
                            };
                        }
                    )
                    .ToList();

                return result.SetSuccessResult(topServers);
            }
            catch (Exception ex)
            {
                result.Code = "SelectOptimalServerAsync:9";
                result.Message = $"Error selecting server: {ex.Message}";
                _logger.LogError(ex, "Error selecting server for region {RegionId}", regionId);
            }

            return result;
        }

        private double CalculateServerScore(ServerStatusData server)
        {
            // Base capacity
            double score = server.MaxConcurrentCallsCount;

            // Penalize by active calls (weighted higher)
            score -= (server.CurrentActiveCallsCount * 1.5);

            // Penalize by queued calls (weighted lower)
            score -= (server.QueuedCallsCount * 0.7);

            // Penalize high CPU usage
            if (server.CpuUsagePercent > 80)
                score *= (1 - ((server.CpuUsagePercent - 80) / 100));

            // Penalize high memory usage
            if (server.MemoryUsagePercent > 85)
                score *= (1 - ((server.MemoryUsagePercent - 85) / 100));

            return Math.Max(0, score);
        }
    }
}
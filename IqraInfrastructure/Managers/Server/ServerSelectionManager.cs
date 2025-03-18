using IqraCore.Entities.Helper.Region;
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

        public async Task<ServerSelectionResultModel> SelectOptimalServerAsync(string regionId)
        {
            var result = new ServerSelectionResultModel();

            try
            {
                // Get the region data
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null || regionData.DisabledAt != null)
                {
                    result.Message = $"Region not available: {regionId}";
                    _logger.LogWarning("Region not available: {RegionId}", regionId);
                    return result;
                }

                // Get backend servers for the specified region
                var backendServers = regionData.Servers
                    .Where(s => s.Type == RegionServerTypeEnum.Backend && s.DisabledAt == null)
                    .Select(s => s.Endpoint)
                    .ToList();

                if (!backendServers.Any())
                {
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
                    result.Message = "No server status data available";
                    _logger.LogWarning("No server status data available for region: {RegionId}", regionId);
                    return result;
                }

                // Filter out servers in maintenance mode
                serverStatuses = serverStatuses.Where(s => !s.MaintenanceMode).ToList();
                if (!serverStatuses.Any())
                {
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
                    result.Message = "All servers are at capacity";
                    _logger.LogWarning("All servers in region {RegionId} are at capacity", regionId);
                    return result;
                }

                // Select the server with the highest score
                var selectedServer = scoredServers.First().Server;

                // Use a distributed lock to avoid race conditions when updating the server load
                string lockKey = $"lock:server:selection:{selectedServer.ServerId}";
                string lockValue = Guid.NewGuid().ToString();
                if (await _serverLock.AcquireAsync(lockKey, lockValue, TimeSpan.FromSeconds(10)))
                {
                    // Get fresh server status to avoid race conditions
                    var freshStatus = await _serverStatusChannel.GetServerStatusAsync(selectedServer.ServerId);
                    if (freshStatus != null)
                    {
                        // Verify server still has capacity
                        if (freshStatus.CurrentActiveCallsCount < freshStatus.MaxConcurrentCallsCount && !freshStatus.MaintenanceMode)
                        {
                            result.Success = true;
                            result.ServerId = selectedServer.ServerId;
                            result.ServerEndpoint = selectedServer.ServerId; // Currently Server id is the server endpoint, in case server id is different than endpoint, edit here CAUTION
                            result.Score = scoredServers.First().Score;

                            _logger.LogInformation("Selected server {ServerId} with score {Score} for region {RegionId}",
                                result.ServerId, result.Score, regionId);
                        }
                        else
                        {
                            result.Message = "Selected server no longer has capacity";
                            _logger.LogInformation("Selected server {ServerId} no longer has capacity", selectedServer.ServerId);
                        }
                    }
                    else
                    {
                        result.Message = "Selected server status is no longer available";
                        _logger.LogWarning("Selected server {ServerId} status is no longer available", selectedServer.ServerId);
                    }

                    await _serverLock.ReleaseAsync(lockKey, lockValue);
                }
                else
                {
                    result.Message = "Failed to acquire lock for server selection";
                    _logger.LogWarning("Failed to acquire lock for server selection: {ServerId}", selectedServer.ServerId);
                }
            }
            catch (Exception ex)
            {
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
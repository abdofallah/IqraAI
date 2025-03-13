using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Telephony;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server
{
    public class ServerMetricsManager : BackgroundService
    {
        private readonly ILogger<ServerMetricsManager> _logger;
        private readonly ServerStatusManager _serverStatusManager;
        private readonly CallQueueRepository _callQueueRepository;
        private readonly ServerConfig _serverConfig;

        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5);

        public ServerMetricsManager(
            ILogger<ServerMetricsManager> logger,
            ServerStatusManager serverStatusService,
            CallQueueRepository callQueueRepository,
            ServerConfig serverConfig)
        {
            _logger = logger;
            _serverStatusManager = serverStatusService;
            _callQueueRepository = callQueueRepository;
            _serverConfig = serverConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Server Metrics Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Update queued calls count
                    int queuedCallsCount = await _callQueueRepository.GetQueuedCallCountForServerAsync(_serverConfig.ServerId, _serverConfig.RegionId);
                    _serverStatusManager.SetQueuedCalls(queuedCallsCount);

                    // Update and publish server status
                    await _serverStatusManager.UpdateAndPublishStatusAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating server metrics");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("Server Metrics Service stopping");
        }
    }
}
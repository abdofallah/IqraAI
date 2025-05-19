using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics
{
    public class ServerMetricsManager : BackgroundService
    {
        private readonly ILogger<ServerMetricsManager> _logger;
        private readonly ServerMetricsMonitor _serverStatusManager;
        private readonly InboundCallQueueRepository _callQueueRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly ServerConfig _serverConfig;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(100);

        public ServerMetricsManager(
            ILogger<ServerMetricsManager> logger,
            ServerMetricsMonitor serverStatusService,
            InboundCallQueueRepository callQueueRepository,
            ConversationStateRepository conversationStateRepository,
            ServerConfig serverConfig)
        {
            _logger = logger;
            _serverStatusManager = serverStatusService;
            _callQueueRepository = callQueueRepository;
            _conversationStateRepository = conversationStateRepository;
            _serverConfig = serverConfig;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Server Metrics Update Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Update queued calls count
                    long queuedCallsCount = await _callQueueRepository.GetActiveInboundCallCountForProcessingServerAsync(_serverConfig.ServerId, _serverConfig.RegionId);
                    _serverStatusManager.SetQueuedCalls((int)queuedCallsCount);

                    long activeCallCount = await _conversationStateRepository.GetActiveCallCountForServerAsync(_serverConfig.ServerId, _serverConfig.RegionId);
                    _serverStatusManager.SetActiveCallsCount((int)activeCallCount);

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
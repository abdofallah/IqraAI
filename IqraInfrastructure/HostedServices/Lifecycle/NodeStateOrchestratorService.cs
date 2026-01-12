using IqraCore.Entities.App.Enum;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Managers.Node;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.HostedServices.Lifecycle
{
    public class NodeStateOrchestratorService : BackgroundService
    {
        private readonly AppNodeTypeEnum _appNodeType;
        private readonly NodeLifecycleManager _lifecycleManager;
        private readonly IqraAppManager _appManager;
        private readonly ILogger<NodeStateOrchestratorService> _logger;

        // Check frequently to react to "Stop" signals quickly
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        public NodeStateOrchestratorService(
            AppNodeTypeEnum appNodeType,
            NodeLifecycleManager lifecycleManager,
            IqraAppManager appManager,
            ILogger<NodeStateOrchestratorService> logger)
        {
            _appNodeType = appNodeType;
            _lifecycleManager = lifecycleManager;
            _appManager = appManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _appManager.RefreshConfigAndStatusAsync();
                    await _lifecycleManager.EvaluateStateAsync();
                    if (_appNodeType == AppNodeTypeEnum.Frontend)
                    {
                        await _appManager.CheckForUpdatesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Node State Orchestration loop.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
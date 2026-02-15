using IqraCore.Entities.App.Enum;
using IqraInfrastructure.HostedServices.Call.Outbound;
using IqraInfrastructure.Managers.Call.Backend;
using IqraInfrastructure.Managers.Node;
using IqraInfrastructure.Managers.Server.Metrics.Monitor;
using IqraInfrastructure.Managers.WebSession;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.HostedServices.Metrics
{
    public class ServerMetricsMonitorService : BackgroundService
    {
        private readonly ILogger<ServerMetricsMonitorService> _logger;
        private readonly ServerMetricsMonitor _serverMetricsMonitor;
        private readonly AppNodeTypeEnum _appNodeType;
        private readonly NodeLifecycleManager _nodeLifecycleManager;
        private readonly bool _doNotLogInstanceStatus;

        // Backend
        private readonly BackendCallProcessorManager? _backendCallProcessorManager;
        private readonly BackendWebSessionProcessorManager? _backendWebSessionProcessorManager;

        // Proxy
        private readonly OutboundCallProcessorService? _outboundCallProcessorService;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(10);

        public ServerMetricsMonitorService(
            IServiceProvider serviceProvider,
            ILogger<ServerMetricsMonitorService> logger,
            AppNodeTypeEnum appNodeType,
            ServerMetricsMonitor serverMetricsMonitor,
            NodeLifecycleManager nodeLifecycleManager,
            bool doNotLogInstanceStatus = false
        ) {
            _logger = logger;
            _appNodeType = appNodeType;
            _serverMetricsMonitor = serverMetricsMonitor;
            _nodeLifecycleManager = nodeLifecycleManager;
            _doNotLogInstanceStatus = doNotLogInstanceStatus;

            if (_appNodeType == AppNodeTypeEnum.Backend)
            {
                if (serverMetricsMonitor is not BackendMetricsMonitor)
                {
                    throw new ArgumentException("ServerMetricsMonitor must be of type BackendMetricsMonitor");
                }

                _backendCallProcessorManager = serviceProvider.GetRequiredService<BackendCallProcessorManager>();
                _backendWebSessionProcessorManager = serviceProvider.GetRequiredService<BackendWebSessionProcessorManager>();
                return;
            }
            else if (_appNodeType == AppNodeTypeEnum.Proxy)
            {
                if (serverMetricsMonitor is not ProxyMetricsMonitor)
                {
                    throw new ArgumentException("ServerMetricsMonitor must be of type ProxyMetricsMonitor");
                }

                _outboundCallProcessorService = serviceProvider.GetRequiredService<OutboundCallProcessorService>();
                return;
            }
            else if (_appNodeType == AppNodeTypeEnum.Unknown)
            {
                throw new ArgumentException($"Unknown app node type: {_appNodeType.ToString()}");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Server Metrics Update Service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Node Specific Metrics
                    if (
                        _appNodeType == AppNodeTypeEnum.Backend &&
                        _serverMetricsMonitor is BackendMetricsMonitor backendMetricsMonitor &&
                        _backendCallProcessorManager != null &&
                        _backendWebSessionProcessorManager != null
                    ) {
                        backendMetricsMonitor.SetActiveTelephonySessionCount(_backendCallProcessorManager.ActiveSessionCount);
                        backendMetricsMonitor.SetActiveWebSessionCount(_backendWebSessionProcessorManager.ActiveSessionCount);
                    }
                    else if (
                        _appNodeType == AppNodeTypeEnum.Proxy &&
                        _serverMetricsMonitor is ProxyMetricsMonitor proxyMetricsMonitor &&
                        _outboundCallProcessorService != null
                    ) {
                        proxyMetricsMonitor.SetCurrentOutboundMarkedQueues(_outboundCallProcessorService.CurrentMarkedCount);
                        proxyMetricsMonitor.SetCurrentOutboundProcessingMarkedQueues(_outboundCallProcessorService.CurrentProcessingMarkedCount);
                        proxyMetricsMonitor.SetCurrentOutboundProcessedMarkedQueues(_outboundCallProcessorService.CurrentProcessedMarkedCount);
                    }

                    // Runtime Status & Reason
                    _serverMetricsMonitor.SetRuntimeStatus(
                        _nodeLifecycleManager.Status,
                        _nodeLifecycleManager.StatusReason
                    );

                    // Update and publish server status
                    if (!_doNotLogInstanceStatus)
                    {
                        await _serverMetricsMonitor.UpdateAndPublishStatusAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating server metrics");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }

            _logger.LogInformation("Server Metrics Service stopping");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Server Metrics Update Service.");
            await base.StopAsync(cancellationToken);

            await _serverMetricsMonitor.ClearCurrentStatusAsync();

            _logger.LogInformation("Server Metrics Update Service stopped and current status cleared.");
        }
    }
}
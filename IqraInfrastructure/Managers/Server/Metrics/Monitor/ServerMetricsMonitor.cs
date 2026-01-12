using IqraCore.Constants;
using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Node.Enum;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Entities.Server.Metrics.Hardware;
using IqraCore.Interfaces.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics.Monitor
{
    public class ServerMetricsMonitor : IAsyncDisposable
    {
        private readonly ILogger<ServerMetricsMonitor> _logger;
        private readonly ServerLiveStatusChannelRepository _serverStatusChannel;
        private readonly ServerStatusRepository _serverStatusRepository;
        private readonly IHardwareMonitor _hardwareMonitor;

        public readonly ServerStatusData _currentStatus;
        public readonly object _statusLock = new object();

        private DateTime _lastHistoricalRecordTime = DateTime.MinValue;
        private readonly TimeSpan _historicalRecordInterval = TimeSpan.FromMinutes(1);

        public ServerMetricsMonitor(
            ILogger<ServerMetricsMonitor> logger,
            ServerStatusData currentStatus,
            ServerLiveStatusChannelRepository serverStatusChannel,
            ServerStatusRepository serverStatusRepository,
            IHardwareMonitor hardwareMonitor
        )
        {
            _logger = logger;
            _serverStatusChannel = serverStatusChannel;
            _serverStatusRepository = serverStatusRepository;
            _hardwareMonitor = hardwareMonitor;
            _currentStatus = currentStatus;

            // Initialize synchronously to ensure state is ready before usage
            InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing ServerStatusManager and Hardware Monitor...");

            await _hardwareMonitor.InitializeAsync();

            // Set Static Info
            lock (_statusLock)
            {
                _currentStatus.Version = IqraGlobalConstants.CurrentAppVersion;
                _currentStatus.RuntimeStatus = NodeRuntimeStatus.Starting;
            }
        }

        public void SetRuntimeStatus(NodeRuntimeStatus status, string reason)
        {
            lock (_statusLock)
            {
                _currentStatus.RuntimeStatus = status;
                _currentStatus.RuntimeStatusReason = reason;
            }
        }

        public Task UpdateAndPublishStatusAsync() => UpdateAndPublishStatusAsync(recordHistorical: true);

        private async Task UpdateAndPublishStatusAsync(bool recordHistorical)
        {
            ServerStatusData statusSnapshot;

            try
            {
                HardwareMetrics metrics = _hardwareMonitor.GetMetrics();

                lock (_statusLock)
                {
                    _currentStatus.CpuUsagePercent = metrics.CpuUsagePercent;
                    _currentStatus.MemoryUsagePercent = metrics.MemoryUsagePercent;
                    _currentStatus.NetworkDownloadMbps = metrics.NetworkDownloadMbps;
                    _currentStatus.NetworkUploadMbps = metrics.NetworkUploadMbps;
                    _currentStatus.LastUpdated = DateTime.UtcNow;

                    statusSnapshot = (ServerStatusData)_currentStatus.Clone();
                }

                await _serverStatusChannel.PublishServerStatusAsync(statusSnapshot);

                var now = statusSnapshot.LastUpdated; // Use snapshot time
                if (recordHistorical && (now - _lastHistoricalRecordTime > _historicalRecordInterval))
                {
                    await _serverStatusRepository.RecordHistoricalStatusAsync(statusSnapshot);
                    _lastHistoricalRecordTime = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating and publishing server status");
            }
        }

        public async Task ClearCurrentStatusAsync()
        {
            try
            {
                if (_currentStatus.Type == AppNodeTypeEnum.Backend && _currentStatus is BackendServerStatusData backendStatus)
                {
                    await _serverStatusChannel.RemoveServerStatusAsync(backendStatus.RegionId, backendStatus.NodeId);
                }
                else if (_currentStatus.Type == AppNodeTypeEnum.Proxy && _currentStatus is ProxyServerStatusData proxyStatus)
                {
                    await _serverStatusChannel.RemoveServerStatusAsync(proxyStatus.RegionId, proxyStatus.NodeId);
                }
                else if (_currentStatus.Type != AppNodeTypeEnum.Unknown)
                {
                    // Singleton nodes (Frontend/Background)
                    await _serverStatusChannel.RemoveServerStatusAsync("singleton", _currentStatus.NodeId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing server status from Redis.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await ClearCurrentStatusAsync();
            _hardwareMonitor?.Dispose();
        }
    }
}
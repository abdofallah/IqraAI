using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Interfaces.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics
{
    public class ServerMetricsMonitor : IAsyncDisposable
    {
        private readonly ILogger<ServerMetricsMonitor> _logger;
        private readonly BackendAppConfig _serverConfig;
        private readonly ServerLiveStatusChannelRepository _serverStatusChannel;
        private readonly ServerStatusRepository _serverStatusRepository;
        private readonly IHardwareMonitor _hardwareMonitor;

        private ServerStatusData _currentStatus;
        private readonly object _statusLock = new object();

        private DateTime _lastHistoricalRecordTime = DateTime.MinValue;
        private readonly TimeSpan _historicalRecordInterval = TimeSpan.FromMinutes(1);

        public ServerMetricsMonitor(
            ILogger<ServerMetricsMonitor> logger,
            BackendAppConfig serverConfig,
            ServerLiveStatusChannelRepository serverStatusChannel,
            ServerStatusRepository serverStatusRepository,
            IHardwareMonitor hardwareMonitor
        )
        {
            _logger = logger;
            _serverConfig = serverConfig;
            _serverStatusChannel = serverStatusChannel;
            _serverStatusRepository = serverStatusRepository;
            _hardwareMonitor = hardwareMonitor;

            _currentStatus = new ServerStatusData
            {
                ServerId = _serverConfig.ServerEndpoint,
                RegionId = _serverConfig.RegionId,
                Type = ServerTypeEnum.Backend, // TODO what if its loaded by proxy?
                LastUpdated = DateTime.UtcNow,
                MaintenanceMode = false,
                MaxConcurrentCallsCount = _serverConfig.ExpectedMaxConcurrentCalls,// todo this should also be dynamic averaged
                CurrentActiveCallsCount = 0,
                QueuedCallsCount = 0,
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                NetworkDownloadMbps = 0,
                NetworkUploadMbps = 0
            };

            InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing ServerStatusManager and Hardware Monitor...");

            await _hardwareMonitor.InitializeAsync();
            await UpdateAndPublishStatusAsync(recordHistorical: false);
        }

        public ServerStatusData GetCurrentStatus()
        {
            lock (_statusLock)
            {
                return (ServerStatusData)_currentStatus.Clone();
            }
        }

        public void SetActiveCallsCount(int count)
        {
            lock (_statusLock)
            {
                _currentStatus.CurrentActiveCallsCount = Math.Max(0, count);
            }
        }

        public void SetQueuedCalls(int count)
        {
            lock (_statusLock)
            {
                _currentStatus.QueuedCallsCount = Math.Max(0, count);
            }
        }

        public void SetMaintenanceMode(bool enabled)
        {
            lock (_statusLock)
            {
                if (_currentStatus.MaintenanceMode != enabled)
                {
                    _currentStatus.MaintenanceMode = enabled;
                    _currentStatus.MaintenanceModeStartedAt = enabled ? DateTime.UtcNow : null;

                    _logger.LogInformation($"Maintenance mode set to {enabled}");
                }
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

        public bool HasCapacity()
        {
            lock (_statusLock)
            {
                return !_currentStatus.MaintenanceMode &&
                       _currentStatus.CurrentActiveCallsCount < _currentStatus.MaxConcurrentCallsCount;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _hardwareMonitor?.Dispose();
            await ValueTask.CompletedTask;
        }
    }
}
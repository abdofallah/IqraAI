using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;
using NickStrupat;
using System.Diagnostics;

namespace IqraInfrastructure.Managers.Server
{
    public class ServerStatusManager
    {
        private readonly ILogger<ServerStatusManager> _logger;
        private readonly ServerConfig _serverConfig;
        private readonly ServerLiveStatusChannelRepository _serverStatusChannel;
        private readonly ServerStatusRepository _serverStatusRepository;

        private ServerStatusData _currentStatus;
        private readonly object _statusLock = new object();

        private DateTime _lastHistoricalRecordTime = DateTime.MinValue;
        private readonly TimeSpan _historicalRecordInterval = TimeSpan.FromMinutes(5);

        private readonly Process _currentProcess;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _ramCounter;

        public ServerStatusManager(
            ILogger<ServerStatusManager> logger,
            ServerConfig serverConfig,
            ServerLiveStatusChannelRepository serverStatusChannel,
            ServerStatusRepository serverStatusRepository)
        {
            _logger = logger;
            _serverConfig = serverConfig;
            _serverStatusChannel = serverStatusChannel;
            _serverStatusRepository = serverStatusRepository;

            // Initialize server status
            _currentStatus = new ServerStatusData
            {
                ServerId = _serverConfig.ServerId,
                RegionId = _serverConfig.RegionId,
                Type = ServerTypeEnum.Backend,
                LastUpdated = DateTime.UtcNow,
                MaintenanceMode = false,
                MaxConcurrentCalls = _serverConfig.MaxConcurrentCalls,
                CurrentActiveCalls = 0,
                QueuedCalls = 0
            };

            // Initialize performance counters if running on Windows
            _currentProcess = Process.GetCurrentProcess();
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters");
            }
        }

        public ServerStatusData GetCurrentStatus()
        {
            lock (_statusLock)
            {
                return _currentStatus;
            }
        }

        public void IncrementActiveCalls()
        {
            lock (_statusLock)
            {
                _currentStatus.CurrentActiveCalls++;
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public void DecrementActiveCalls()
        {
            lock (_statusLock)
            {
                _currentStatus.CurrentActiveCalls = Math.Max(0, _currentStatus.CurrentActiveCalls - 1);
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public void SetQueuedCalls(int count)
        {
            lock (_statusLock)
            {
                _currentStatus.QueuedCalls = count;
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public void SetMaintenanceMode(bool enabled)
        {
            lock (_statusLock)
            {
                _currentStatus.MaintenanceMode = enabled;
                _currentStatus.MaintenanceModeStartedAt = enabled ? DateTime.UtcNow : null;
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public async Task UpdateAndPublishStatusAsync()
        {
            try
            {
                lock (_statusLock)
                {
                    // Update metrics
                    UpdateHardwareMetrics();
                    _currentStatus.LastUpdated = DateTime.UtcNow;
                }

                // Get a snapshot of the current status
                ServerStatusData statusSnapshot;
                lock (_statusLock)
                {
                    statusSnapshot = _currentStatus.Clone();
                }

                // Publish to Redis
                await _serverStatusChannel.PublishServerStatusAsync(statusSnapshot);

                // Update MongoDB (less frequently)
                if (DateTime.UtcNow - _lastHistoricalRecordTime > _historicalRecordInterval)
                {
                    await _serverStatusRepository.RecordHistoricalStatusAsync(statusSnapshot);
                    _lastHistoricalRecordTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating and publishing server status");
            }
        }

        private void UpdateHardwareMetrics()
        {
            try
            {
                // Update CPU usage
                if (OperatingSystem.IsWindows() && _cpuCounter != null)
                {
                    _currentStatus.CpuUsagePercent = _cpuCounter.NextValue();
                }
                else
                {
                    _currentStatus.CpuUsagePercent = GetCpuUsageAlternative();
                }

                // Update memory usage
                if (OperatingSystem.IsWindows() && _ramCounter != null)
                {
                    var availableMb = _ramCounter.NextValue();
                    var totalMb = new ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
                    _currentStatus.MemoryUsagePercent = 100 - (availableMb / totalMb * 100);
                }
                else
                {
                    _currentStatus.MemoryUsagePercent = GetMemoryUsageAlternative();
                }

                // Update network usage (This is a simplified approach)
                _currentStatus.NetworkUsageMbps = 0; // Implement if needed
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating hardware metrics");
            }
        }

        private double GetCpuUsageAlternative()
        {
            try
            {
                // Use process CPU time as an approximation
                TimeSpan processorTime = _currentProcess.TotalProcessorTime;
                return processorTime.TotalMilliseconds / (Environment.ProcessorCount * 100);
            }
            catch
            {
                return 0;
            }
        }

        private double GetMemoryUsageAlternative()
        {
            try
            {
                // Use process working set as an approximation
                return _currentProcess.WorkingSet64 / (double)(new ComputerInfo().TotalPhysicalMemory) * 100;
            }
            catch
            {
                return 0;
            }
        }

        public bool HasCapacity()
        {
            lock (_statusLock)
            {
                return !_currentStatus.MaintenanceMode &&
                       _currentStatus.CurrentActiveCalls < _currentStatus.MaxConcurrentCalls;
            }
        }
    }
}
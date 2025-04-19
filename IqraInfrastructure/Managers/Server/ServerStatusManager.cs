using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;
using NickStrupat; // Assuming this is for ComputerInfo
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        private readonly ComputerInfo? _computerInfo;

        // --- Linux Specific Fields ---
        private const string ProcStatPath = "/proc/stat";
        private const string ProcMemInfoPath = "/proc/meminfo";
        private long _previousTotalCpuTime = 0;
        private long _previousIdleCpuTime = 0;
        // ---------------------------

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

            _currentStatus = new ServerStatusData
            {
                ServerId = _serverConfig.ServerId,
                RegionId = _serverConfig.RegionId,
                Type = ServerTypeEnum.Backend,
                LastUpdated = DateTime.UtcNow,
                MaintenanceMode = false,
                MaxConcurrentCallsCount = _serverConfig.ExpectedMaxConcurrentCalls,
                CurrentActiveCallsCount = 0,
                QueuedCallsCount = 0,
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                NetworkUsageMbps = 0
            };

            _currentProcess = Process.GetCurrentProcess();
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                    _computerInfo = new ComputerInfo();
                    _cpuCounter.NextValue(); // Prime the counter
                    Task.Delay(100).Wait(); // Brief delay helps accuracy on first real read
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Prime the CPU reading for the first calculation delta
                    ReadLinuxCpuTimes(out _previousTotalCpuTime, out _previousIdleCpuTime);
                }
                else
                {
                    _logger.LogWarning("Operating system is not Windows or Linux. Hardware metrics might be limited or inaccurate.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize OS-specific performance monitoring components.");
                _cpuCounter = null;
                _ramCounter = null;
                _computerInfo = null;
            }
        }

        public ServerStatusData GetCurrentStatus()
        {
            lock (_statusLock)
            {
                return _currentStatus.Clone(); // Return a clone
            }
        }

        public void IncrementActiveCalls()
        {
            lock (_statusLock)
            {
                _currentStatus.CurrentActiveCallsCount++;
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public void DecrementActiveCalls()
        {
            lock (_statusLock)
            {
                _currentStatus.CurrentActiveCallsCount = Math.Max(0, _currentStatus.CurrentActiveCallsCount - 1);
                _currentStatus.LastUpdated = DateTime.UtcNow;
            }
        }

        public void SetQueuedCalls(int count)
        {
            lock (_statusLock)
            {
                _currentStatus.QueuedCallsCount = count;
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
            ServerStatusData statusSnapshot;

            try
            {
                lock (_statusLock)
                {
                    UpdateHardwareMetrics();
                    _currentStatus.LastUpdated = DateTime.UtcNow;
                    statusSnapshot = _currentStatus.Clone();
                } // Lock released here

                // Publish to Redis
                await _serverStatusChannel.PublishServerStatusAsync(statusSnapshot);

                // Update MongoDB (less frequently)
                var now = DateTime.UtcNow;
                if (now - _lastHistoricalRecordTime > _historicalRecordInterval)
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

        private void UpdateHardwareMetrics()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    if (_cpuCounter != null)
                    {
                        _currentStatus.CpuUsagePercent = _cpuCounter.NextValue();
                    }
                    else { _currentStatus.CpuUsagePercent = 0; }

                    if (_ramCounter != null && _computerInfo != null)
                    {
                        var availableMb = _ramCounter.NextValue();
                        try
                        {
                            var totalMb = (double)_computerInfo.TotalPhysicalMemory / 1024 / 1024;
                            if (totalMb > 0)
                            {
                                _currentStatus.MemoryUsagePercent = 100.0 * (totalMb - availableMb) / totalMb;
                            }
                            else { _currentStatus.MemoryUsagePercent = 0; }
                        }
                        catch (Exception memEx)
                        {
                            _logger.LogWarning(memEx, "Could not retrieve total physical memory using ComputerInfo.");
                            _currentStatus.MemoryUsagePercent = 0;
                        }
                    }
                    else { _currentStatus.MemoryUsagePercent = 0; }
                }
                else if (OperatingSystem.IsLinux())
                {
                    _currentStatus.CpuUsagePercent = GetLinuxCpuUsage();
                    _currentStatus.MemoryUsagePercent = GetLinuxMemoryUsage();
                }

                _currentStatus.CpuUsagePercent = Math.Clamp(_currentStatus.CpuUsagePercent, 0.0, 100.0);
                _currentStatus.MemoryUsagePercent = Math.Clamp(_currentStatus.MemoryUsagePercent, 0.0, 100.0);

                // Network usage remains unimplemented
                _currentStatus.NetworkUsageMbps = 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating hardware metrics");
                _currentStatus.CpuUsagePercent = 0;
                _currentStatus.MemoryUsagePercent = 0;
                _currentStatus.NetworkUsageMbps = 0;
            }
        }

        // --- Linux Specific Methods ---

        private bool ReadLinuxCpuTimes(out long totalTime, out long idleTime)
        {
            totalTime = 0;
            idleTime = 0;
            try
            {
                string? cpuLine = File.ReadLines(ProcStatPath).FirstOrDefault();
                if (cpuLine == null || !cpuLine.StartsWith("cpu "))
                {
                    _logger.LogWarning($"Could not read expected CPU line from {ProcStatPath}");
                    return false;
                }

                var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 8)
                {
                    _logger.LogWarning($"Unexpected format in {ProcStatPath}: {cpuLine}");
                    return false;
                }

                for (int i = 1; i < parts.Length; i++)
                {
                    if (long.TryParse(parts[i], out long time)) { totalTime += time; }
                    else
                    {
                        _logger.LogWarning($"Could not parse CPU time value '{parts[i]}' from {ProcStatPath}");
                        return false;
                    }
                }

                // Idle time = idle (index 4) + iowait (index 5)
                if (long.TryParse(parts[4], out long idle) && long.TryParse(parts[5], out long ioWait))
                {
                    idleTime = idle + ioWait;
                }
                else
                {
                    _logger.LogWarning($"Could not parse idle/iowait time values from {ProcStatPath}");
                    return false;
                }
                return true;
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, $"IO Error reading {ProcStatPath}. Check file existence and permissions.");
                return false;
            }
            catch (UnauthorizedAccessException authEx)
            {
                _logger.LogWarning(authEx, $"Permission denied reading {ProcStatPath}.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error reading CPU times from {ProcStatPath}.");
                return false;
            }
        }

        private double GetLinuxCpuUsage()
        {
            if (!ReadLinuxCpuTimes(out long currentTotalTime, out long currentIdleTime))
            {
                _logger.LogWarning("Could not read current Linux CPU times. Reporting 0% usage.");
                return 0.0;
            }

            long deltaTotal = currentTotalTime - _previousTotalCpuTime;
            long deltaIdle = currentIdleTime - _previousIdleCpuTime;

            _previousTotalCpuTime = currentTotalTime;
            _previousIdleCpuTime = currentIdleTime;

            if (deltaTotal <= 0)
            {
                return 0.0; // Cannot calculate delta accurately yet or error.
            }

            double cpuUsage = (double)(deltaTotal - deltaIdle) / deltaTotal * 100.0;
            return cpuUsage;
        }

        private double GetLinuxMemoryUsage()
        {
            try
            {
                long memTotal = -1, memFree = -1, buffers = -1, cached = -1, sReclaimable = -1;
                var lines = File.ReadAllLines(ProcMemInfoPath);

                foreach (var line in lines)
                {
                    var parts = line.Split(':', StringSplitOptions.TrimEntries);
                    if (parts.Length != 2) continue;
                    string key = parts[0];
                    string value = parts[1].Replace(" kB", "", StringComparison.OrdinalIgnoreCase).Trim();

                    if (long.TryParse(value, out long parsedValue))
                    {
                        switch (key)
                        {
                            case "MemTotal": memTotal = parsedValue; break;
                            case "MemFree": memFree = parsedValue; break;
                            case "Buffers": buffers = parsedValue; break;
                            case "Cached": cached = parsedValue; break;
                            case "SReclaimable": sReclaimable = parsedValue; break; // Slab Reclaimable
                        }
                    }
                }

                if (memTotal <= 0 || memFree < 0 || buffers < 0 || cached < 0)
                {
                    _logger.LogWarning($"Could not parse required memory fields from {ProcMemInfoPath}.");
                    return 0.0;
                }

                // Calculate used memory. Linux uses free memory + buffers + cache for available.
                // Used = Total - Free - Buffers - Cache.
                // More accurate: Used = Total - Free - Buffers - (Cached - SReclaimable)
                long usedMemory;
                if (sReclaimable > 0)
                {
                    usedMemory = memTotal - memFree - buffers - (cached - sReclaimable);
                }
                else
                {
                    usedMemory = memTotal - memFree - buffers - cached; // Standard calc
                }

                double memUsagePercent = (double)usedMemory / memTotal * 100.0;
                return memUsagePercent;
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, $"IO Error reading {ProcMemInfoPath}.");
                return 0.0;
            }
            catch (UnauthorizedAccessException authEx)
            {
                _logger.LogWarning(authEx, $"Permission denied reading {ProcMemInfoPath}.");
                return 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error reading memory info from {ProcMemInfoPath}.");
                return 0.0;
            }
        }

        // --- Other Methods ---

        public bool HasCapacity()
        {
            lock (_statusLock)
            {
                return !_currentStatus.MaintenanceMode && _currentStatus.CurrentActiveCallsCount < _currentStatus.MaxConcurrentCallsCount;
            }
        }
    }
}
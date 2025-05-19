using IqraCore.Entities.Server.Metrics;
using IqraCore.Entities.Server;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using IqraCore.Interfaces.Server;

namespace IqraInfrastructure.Managers.Server.Metrics
{
    public class WindowsHardwareMonitor : IHardwareMonitor
    {
        private readonly ILogger<WindowsHardwareMonitor> _logger;
        private readonly BackendAppConfig _serverConfig;

        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _memCounter;
        private PerformanceCounter? _networkRecvCounter;
        private PerformanceCounter? _networkSentCounter;

        private long _totalMemoryBytes = 0;

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        public WindowsHardwareMonitor(ILogger<WindowsHardwareMonitor> logger, BackendAppConfig serverConfig)
        {
            _logger = logger;
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));

            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("WindowsHardwareMonitor can only run on Windows.");
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Memory
                if (GetPhysicallyInstalledSystemMemory(out long memKb))
                {
                    _totalMemoryBytes = memKb * 1024;
                }
                else
                {
                    _logger.LogWarning("Could not get total physical memory via kernel32.dll.");
                }
                _memCounter = new PerformanceCounter("Memory", "Available MBytes");
                _memCounter.NextValue(); // Prime

                // CPU - Using "Processor Information" and "% Processor Utility" is often more accurate for modern CPUs
                _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                _cpuCounter.NextValue(); // Prime

                // Network
                if (string.IsNullOrWhiteSpace(_serverConfig.NetworkInterfaceName))
                {
                    _logger.LogWarning("ServerConfig.NetworkInterfaceName is not set. Network monitoring will be disabled.");
                }
                else
                {
                    // Verify the instance exists
                    if (PerformanceCounterCategory.InstanceExists(_serverConfig.NetworkInterfaceName, "Network Interface"))
                    {
                        _networkRecvCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", _serverConfig.NetworkInterfaceName);
                        _networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", _serverConfig.NetworkInterfaceName);
                        _networkRecvCounter.NextValue(); // Prime
                        _networkSentCounter.NextValue(); // Prime
                    }
                    else
                    {
                        _logger.LogWarning($"Network interface instance '{_serverConfig.NetworkInterfaceName}' not found. Network monitoring disabled.");
                        _networkRecvCounter = null;
                        _networkSentCounter = null;
                    }
                }

                // Allow counters time to stabilize after priming
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Windows performance counters.");
                // Ensure counters are nullified if initialization failed partially
                _cpuCounter?.Dispose();
                _memCounter?.Dispose();
                _networkRecvCounter?.Dispose();
                _networkSentCounter?.Dispose();
                _cpuCounter = null;
                _memCounter = null;
                _networkRecvCounter = null;
                _networkSentCounter = null;
                _totalMemoryBytes = 0;
            }
        }

        public HardwareMetrics GetMetrics()
        {
            var metrics = new HardwareMetrics();

            try
            {
                // CPU
                metrics.CpuUsagePercent = _cpuCounter?.NextValue() ?? 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read CPU usage counter.");
                metrics.CpuUsagePercent = 0.0;
            }

            try
            {
                // Memory
                if (_memCounter != null && _totalMemoryBytes > 0)
                {
                    float availableBytes = _memCounter.NextValue() * 1024 * 1024; // Available MBytes -> Bytes
                    float usedBytes = _totalMemoryBytes - availableBytes;
                    metrics.MemoryUsagePercent = (double)(usedBytes / _totalMemoryBytes) * 100.0;
                }
                else
                {
                    metrics.MemoryUsagePercent = 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Memory usage counter.");
                metrics.MemoryUsagePercent = 0.0;
            }

            try
            {
                // Network
                if (_networkRecvCounter != null && _networkSentCounter != null)
                {
                    float recvBytesPerSec = _networkRecvCounter.NextValue();
                    float sentBytesPerSec = _networkSentCounter.NextValue();

                    metrics.NetworkDownloadMbps = (recvBytesPerSec * 8) / (1024.0 * 1024.0); // Bytes/s to Mbps
                    metrics.NetworkUploadMbps = (sentBytesPerSec * 8) / (1024.0 * 1024.0); // Bytes/s to Mbps
                }
                else
                {
                    metrics.NetworkDownloadMbps = 0.0;
                    metrics.NetworkUploadMbps = 0.0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read Network usage counters.");
                metrics.NetworkDownloadMbps = 0.0;
                metrics.NetworkUploadMbps = 0.0;
            }

            // Clamp values
            metrics.CpuUsagePercent = Math.Clamp(metrics.CpuUsagePercent, 0.0, 100.0);
            metrics.MemoryUsagePercent = Math.Clamp(metrics.MemoryUsagePercent, 0.0, 100.0);
            metrics.NetworkDownloadMbps = Math.Max(0.0, metrics.NetworkDownloadMbps);
            metrics.NetworkUploadMbps = Math.Max(0.0, metrics.NetworkUploadMbps);


            return metrics;
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _memCounter?.Dispose();
            _networkRecvCounter?.Dispose();
            _networkSentCounter?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
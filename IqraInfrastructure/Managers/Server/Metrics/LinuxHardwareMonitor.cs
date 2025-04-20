using IqraCore.Entities.Server.Metrics;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics
{
    public class LinuxHardwareMonitor : IHardwareMonitor
    {
        private readonly ILogger<LinuxHardwareMonitor> _logger;
        private readonly ServerConfig _serverConfig;

        private const string ProcStatPath = "/proc/stat";
        private const string ProcMemInfoPath = "/proc/meminfo";
        private string? _networkRxBytesPath;
        private string? _networkTxBytesPath;

        // CPU State
        private long _previousTotalCpuTime = 0;
        private long _previousIdleCpuTime = 0;

        // Network State
        private long _previousRxBytes = 0;
        private long _previousTxBytes = 0;
        private DateTime _lastNetworkReadTime = DateTime.MinValue;

        public LinuxHardwareMonitor(ILogger<LinuxHardwareMonitor> logger, ServerConfig serverConfig)
        {
            _logger = logger;
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));

            if (!OperatingSystem.IsLinux())
            {
                throw new PlatformNotSupportedException("LinuxHardwareMonitor can only run on Linux.");
            }

            if (string.IsNullOrWhiteSpace(serverConfig.NetworkInterfaceName))
            {
                _logger.LogWarning("ServerConfig.NetworkInterfaceName is not set. Network monitoring will be disabled.");
            }
            else
            {
                _networkRxBytesPath = $"/sys/class/net/{serverConfig.NetworkInterfaceName}/statistics/rx_bytes";
                _networkTxBytesPath = $"/sys/class/net/{serverConfig.NetworkInterfaceName}/statistics/tx_bytes";
            }
        }

        public Task InitializeAsync()
        {
            // Prime CPU reading
            if (!ReadLinuxCpuTimes(out _previousTotalCpuTime, out _previousIdleCpuTime))
            {
                _logger.LogWarning("Initial read of Linux CPU times failed.");
            }

            // Prime Network reading
            if (_networkRxBytesPath != null && _networkTxBytesPath != null)
            {
                ReadNetworkBytes(out _previousRxBytes, out _previousTxBytes);
                _lastNetworkReadTime = DateTime.UtcNow;
            }

            return Task.CompletedTask; // No async operations needed for initialization here
        }

        public HardwareMetrics GetMetrics()
        {
            return new HardwareMetrics
            {
                CpuUsagePercent = GetLinuxCpuUsage(),
                MemoryUsagePercent = GetLinuxMemoryUsage(),
                NetworkDownloadMbps = GetLinuxNetworkRate(true), // true for Download (rx)
                NetworkUploadMbps = GetLinuxNetworkRate(false) // false for Upload (tx)
            };
        }

        // --- CPU ---
        private bool ReadLinuxCpuTimes(out long totalTime, out long idleTime)
        {
            totalTime = 0;
            idleTime = 0;
            try
            {
                string? cpuLine = File.ReadLines(ProcStatPath).FirstOrDefault();
                if (cpuLine == null || !cpuLine.StartsWith("cpu ")) return false;

                // user nice system idle iowait irq softirq steal guest guest_nice
                //   0    1    2     3    4     5     6      7     8     9
                var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 8) return false; // Need at least up to iowait

                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long iowait = long.Parse(parts[5]);
                long irq = long.Parse(parts[6]);
                long softirq = long.Parse(parts[7]);
                // Optional: steal, guest, guest_nice (added in later kernels)
                long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;
                // Note: Guest times are already included in user/nice, so don't double add if calculating total non-idle

                // Total time = sum of all times
                totalTime = user + nice + system + idle + iowait + irq + softirq + steal;
                // Idle time = idle + iowait (time CPU was waiting for I/O)
                idleTime = idle + iowait;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading CPU times from {ProcStatPath}.");
                return false;
            }
        }

        private double GetLinuxCpuUsage()
        {
            if (!ReadLinuxCpuTimes(out long currentTotalTime, out long currentIdleTime))
            {
                return 0.0; // Error reading current times
            }

            long deltaTotal = currentTotalTime - _previousTotalCpuTime;
            long deltaIdle = currentIdleTime - _previousIdleCpuTime;

            // Update previous values for the next calculation
            _previousTotalCpuTime = currentTotalTime;
            _previousIdleCpuTime = currentIdleTime;

            if (deltaTotal <= 0)
            {
                // Avoid division by zero or negative time warp; need another sample.
                return 0.0;
            }

            // Usage = (Total Time - Idle Time) / Total Time
            double usage = (double)(deltaTotal - deltaIdle) / deltaTotal * 100.0;
            return Math.Clamp(usage, 0.0, 100.0);
        }

        // --- Memory ---
        private double GetLinuxMemoryUsage()
        {
            try
            {
                long memTotal = -1, memFree = -1, buffers = -1, cached = -1, sReclaimable = -1;
                var lines = File.ReadAllLines(ProcMemInfoPath);

                foreach (var line in lines)
                {
                    var parts = line.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2) continue;
                    string key = parts[0];
                    if (parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } valueParts)
                    {
                        if (long.TryParse(valueParts[0], out long parsedValue))
                        {
                            switch (key)
                            {
                                case "MemTotal": memTotal = parsedValue; break;
                                case "MemFree": memFree = parsedValue; break;
                                case "Buffers": buffers = parsedValue; break;
                                case "Cached": cached = parsedValue; break;
                                case "SReclaimable": sReclaimable = parsedValue; break;
                            }
                        }
                    }
                }

                if (memTotal <= 0 || memFree < 0 || buffers < 0 || cached < 0)
                {
                    _logger.LogWarning($"Could not parse required memory fields from {ProcMemInfoPath}.");
                    return 0.0;
                }

                // More modern/accurate calculation for available memory:
                // Available = MemFree + Buffers + Cached + SReclaimable - Shmem (if available, else ignore)
                // Used = MemTotal - Available
                // Simpler common calculation (Used = Total - Free - Buffers - Cached):
                long memUsed = memTotal - memFree - buffers - (sReclaimable > 0 ? (cached - sReclaimable) : cached);
                // Handle cases where SReclaimable isn't present or is 0
                if (sReclaimable <= 0)
                {
                    memUsed = memTotal - memFree - buffers - cached;
                }
                else
                {
                    memUsed = memTotal - memFree - buffers - cached; // Simple version still common
                                                                     // More precise: Available = MemFree + Buffers + Cached + SReclaimable
                                                                     // memUsed = memTotal - (memFree + buffers + cached + sReclaimable);
                }


                if (memTotal <= 0) return 0.0; // Avoid division by zero

                double memUsagePercent = (double)memUsed / memTotal * 100.0;
                return Math.Clamp(memUsagePercent, 0.0, 100.0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading memory info from {ProcMemInfoPath}.");
                return 0.0;
            }
        }

        // --- Network ---
        private bool ReadNetworkBytes(out long rxBytes, out long txBytes)
        {
            rxBytes = 0;
            txBytes = 0;
            bool rxOk = false;
            bool txOk = false;

            if (string.IsNullOrEmpty(_networkRxBytesPath) || string.IsNullOrEmpty(_networkTxBytesPath)) return false;

            try
            {
                if (File.Exists(_networkRxBytesPath))
                {
                    string rxStr = File.ReadAllText(_networkRxBytesPath).Trim();
                    if (long.TryParse(rxStr, out rxBytes)) rxOk = true;
                    else _logger.LogWarning($"Could not parse long from {_networkRxBytesPath}: '{rxStr}'");
                }
                else _logger.LogWarning($"Network stats file not found: {_networkRxBytesPath}");

                if (File.Exists(_networkTxBytesPath))
                {
                    string txStr = File.ReadAllText(_networkTxBytesPath).Trim();
                    if (long.TryParse(txStr, out txBytes)) txOk = true;
                    else _logger.LogWarning($"Could not parse long from {_networkTxBytesPath}: '{txStr}'");
                }
                else _logger.LogWarning($"Network stats file not found: {_networkTxBytesPath}");

                return rxOk && txOk;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error reading network stats from /sys/class/net/{_serverConfig.NetworkInterfaceName}/statistics");
                return false;
            }
        }

        private double GetLinuxNetworkRate(bool isDownload)
        {
            if (string.IsNullOrEmpty(_networkRxBytesPath) || string.IsNullOrEmpty(_networkTxBytesPath))
            {
                return 0.0; // Network monitoring not configured or interface name missing
            }

            if (!ReadNetworkBytes(out long currentRxBytes, out long currentTxBytes))
            {
                return 0.0; // Failed to read current byte counts
            }

            var now = DateTime.UtcNow;
            var timeDelta = now - _lastNetworkReadTime;

            // Store current values for next calculation *before* calculating rate
            long prevRx = _previousRxBytes;
            long prevTx = _previousTxBytes;
            _previousRxBytes = currentRxBytes;
            _previousTxBytes = currentTxBytes;
            _lastNetworkReadTime = now;

            if (timeDelta.TotalSeconds <= 0)
            {
                return 0.0; // Time hasn't passed or went backward, cannot calculate rate
            }

            long bytesDelta = isDownload ? (currentRxBytes - prevRx) : (currentTxBytes - prevTx);

            // Handle counter wrap-around (unlikely with 64-bit counters, but good practice)
            if (bytesDelta < 0)
            {
                bytesDelta = 0; // Assume wrap-around or reset, report 0 for this interval
                _logger.LogWarning($"Network counter wrap-around detected for {(isDownload ? "RX" : "TX")}.");
            }

            double bytesPerSecond = bytesDelta / timeDelta.TotalSeconds;
            double mbps = (bytesPerSecond * 8) / (1024.0 * 1024.0); // Bytes/s to Mbps

            return Math.Max(0.0, mbps); // Ensure non-negative
        }


        public void Dispose()
        {
            // No explicit resources to dispose in this Linux implementation
            GC.SuppressFinalize(this);
        }
    }
}
using IqraCore.Constants;
using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Node.Enum;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Interfaces.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics.Monitor
{
    public class BackendMetricsMonitor : ServerMetricsMonitor
    {
        public BackendMetricsMonitor(
            ILogger<ServerMetricsMonitor> logger,
            ServerLiveStatusChannelRepository serverStatusChannel,
            ServerStatusRepository serverStatusRepository,
            IHardwareMonitor hardwareMonitor,
            BackendAppConfig serverConfig
        ) : base(logger, BuildServerStatusData(serverConfig), serverStatusChannel, serverStatusRepository, hardwareMonitor)
        { }

        private BackendServerStatusData _backendServerStatusData
        {
            get
            {
                lock (_statusLock)
                {
                    return (BackendServerStatusData)_currentStatus;
                }
            }
        }

        private static BackendServerStatusData BuildServerStatusData(BackendAppConfig serverConfig)
        {
            return new BackendServerStatusData
            {
                NodeId = serverConfig.Id,
                RegionId = serverConfig.RegionId,
                Type = AppNodeTypeEnum.Backend,

                // Base Fields
                Version = IqraGlobalConstants.CurrentAppVersion,
                RuntimeStatus = NodeRuntimeStatus.Starting,
                LastUpdated = DateTime.UtcNow,

                // Specific Fields
                MaxConcurrentCallsCount = serverConfig.ExpectedMaxConcurrentCalls,
                CurrentActiveTelephonySessionCount = 0,
                CurrentActiveWebSessionCount = 0,

                // Metrics Defaults
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                NetworkDownloadMbps = 0,
                NetworkUploadMbps = 0
            };
        }

        public void SetActiveTelephonySessionCount(int count)
        {
            lock (_statusLock)
            {
                _backendServerStatusData.CurrentActiveTelephonySessionCount = Math.Max(0, count);
            }
        }

        public void SetActiveWebSessionCount(int count)
        {
            lock (_statusLock)
            {
                _backendServerStatusData.CurrentActiveWebSessionCount = Math.Max(0, count);
            }
        }

        public bool HasCapacity()
        {
            lock (_statusLock)
            {
                return _backendServerStatusData.CurrentActiveTelephonySessionCount < _backendServerStatusData.MaxConcurrentCallsCount;
            }
        }
    }
}

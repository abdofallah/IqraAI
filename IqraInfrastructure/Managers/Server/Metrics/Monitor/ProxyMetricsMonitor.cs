using IqraCore.Constants;
using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Node.Enum;
using IqraCore.Entities.Server.Configuration;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Interfaces.Server;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Server.Metrics.Monitor
{
    public class ProxyMetricsMonitor : ServerMetricsMonitor
    {
        public ProxyMetricsMonitor(
            ILogger<ProxyMetricsMonitor> logger,
            ServerLiveStatusChannelRepository serverStatusChannel,
            ServerStatusRepository serverStatusRepository,
            IHardwareMonitor hardwareMonitor,
            ProxyAppConfig serverConfig
        ) : base(logger, BuildServerStatusData(serverConfig), serverStatusChannel, serverStatusRepository, hardwareMonitor)
        { }

        private ProxyServerStatusData _proxyServerStatusData
        {
            get
            {
                lock (_statusLock)
                {
                    return (ProxyServerStatusData)_currentStatus;
                }
            }
        }

        private static ProxyServerStatusData BuildServerStatusData(ProxyAppConfig serverConfig)
        {
            return new ProxyServerStatusData
            {
                NodeId = serverConfig.ServerId,
                RegionId = serverConfig.RegionId,
                Type = AppNodeTypeEnum.Proxy,

                // Base Fields
                Version = IqraGlobalConstants.CurrentAppVersion,
                RuntimeStatus = NodeRuntimeStatus.Starting,
                LastUpdated = DateTime.UtcNow,

                // Specific Fields
                CurrentOutboundMarkedQueues = 0,
                CurrentOutboundProcessingMarkedQueues = 0,
                CurrentOutboundProcessedMarkedQueues = 0,

                // Metrics Defaults
                CpuUsagePercent = 0,
                MemoryUsagePercent = 0,
                NetworkDownloadMbps = 0,
                NetworkUploadMbps = 0
            };
        }

        public void SetCurrentOutboundMarkedQueues(int count)
        {
            lock (_statusLock)
            {
                _proxyServerStatusData.CurrentOutboundMarkedQueues = Math.Max(0, count);
            }
        }

        public void SetCurrentOutboundProcessingMarkedQueues(int count)
        {
            lock (_statusLock)
            {
                _proxyServerStatusData.CurrentOutboundProcessingMarkedQueues = Math.Max(0, count);
            }
        }

        public void SetCurrentOutboundProcessedMarkedQueues(int count)
        {
            lock (_statusLock)
            {
                _proxyServerStatusData.CurrentOutboundProcessedMarkedQueues = Math.Max(0, count);
            }
        }
    }
}

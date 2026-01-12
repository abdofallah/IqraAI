using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraInfrastructure.Repositories.Server;

namespace IqraInfrastructure.Managers.Server.Metrics
{
    public class ServerMetricsManager
    {
        private readonly ServerLiveStatusChannelRepository _serverLiveStatusChannel;
        private readonly ServerStatusRepository _serverStatusRepository;

        public ServerMetricsManager(
            ServerLiveStatusChannelRepository serverLiveStatusChannel,
            ServerStatusRepository serverStatusRepository
        ) {
            _serverLiveStatusChannel = serverLiveStatusChannel;
            _serverStatusRepository = serverStatusRepository;
        }

        public async Task<ServerStatusData?> GetServerStatusData(string regionId, string nodeId)
        {
            return await _serverLiveStatusChannel.GetServerStatusAsync(regionId, nodeId);
        }

        public async Task<BackendServerStatusData?> GetLiveBackendServerStatusData(string regionId, string nodeId)
        {
            var result = await _serverLiveStatusChannel.GetServerStatusAsync(regionId, nodeId);

            if (result is BackendServerStatusData backendStatus)
            {
                return backendStatus;
            }

            return null;
        }

        public async Task<bool> CheckProxyNodeRunning(string regionId, string nodeId)
        {
            var result = await _serverLiveStatusChannel.GetServerStatusAsync(regionId, nodeId);
            return result is ProxyServerStatusData && result != null;
        }

        public async Task<bool> CheckBackendNodeRunning(string regionId, string nodeId)
        {
            var result = await _serverLiveStatusChannel.GetServerStatusAsync(regionId, nodeId);
            return result is BackendServerStatusData && result != null;
        }

        public async Task<bool> CheckAnyBackgroundNodeRunning()
        {
            var result = await _serverLiveStatusChannel.GetServerStatusAsync("singleton", "Background");
            return result is not null;
        }

        public async Task<bool> CheckAnyFrontendNodeRunning()
        {
            var result = await _serverLiveStatusChannel.GetServerStatusAsync("singleton", "Frontend");
            return result is not null;
        }

        public async Task<(bool, int)> AreAnyWorkerNodesRunningAndCount()
        {
            var nodes = await _serverLiveStatusChannel.GetAllActiveNodesAsync();

            var count = nodes.Count(n => n.Type == AppNodeTypeEnum.Backend ||
                                  n.Type == AppNodeTypeEnum.Proxy ||
                                  n.Type == AppNodeTypeEnum.Background);

            return (count > 0, count);
        }

        public async Task<List<ServerStatusData>> GetAllActiveNodesAsync()
        {
            return await _serverLiveStatusChannel.GetAllActiveNodesAsync();
        }

        public async Task<Dictionary<string, ServerStatusData>> GetAllActiveNodesMapAsync()
        {
            var list = await _serverLiveStatusChannel.GetAllActiveNodesAsync();
            var dict = new Dictionary<string, ServerStatusData>();
            foreach (var item in list)
            {
                dict.TryAdd(item.NodeId, item);
            }
            return dict;
        }
    }
}

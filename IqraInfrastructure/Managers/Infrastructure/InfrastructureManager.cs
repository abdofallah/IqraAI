using IqraCore.Entities.App.Enum;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Node.Enum;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Models.Infrastructure;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Infrastructure
{
    public class InfrastructureManager
    {
        private readonly RegionManager _regionManager;
        private readonly ServerMetricsManager _metricsManager;
        private readonly ServerStatusRepository _historyRepo;
        private readonly ILogger<InfrastructureManager> _logger;

        public InfrastructureManager(
            RegionManager regionManager,
            ServerMetricsManager metricsManager,
            ServerStatusRepository historyRepo,
            ILogger<InfrastructureManager> logger)
        {
            _regionManager = regionManager;
            _metricsManager = metricsManager;
            _historyRepo = historyRepo;
            _logger = logger;
        }

        public async Task<FunctionReturnResult<InfrastructureOverviewModel>> GetOverviewAsync()
        {
            var result = new FunctionReturnResult<InfrastructureOverviewModel>();
            try
            {
                // 1. Fetch Data Parallel
                var regionsTask = _regionManager.GetRegions();
                var liveNodesTask = _metricsManager.GetAllActiveNodesAsync();

                await Task.WhenAll(regionsTask, liveNodesTask);

                var regions = regionsTask.Result.Data ?? new List<RegionData>();
                var liveNodes = liveNodesTask.Result;

                // 2. Calculate Global Counts
                // Configured (MongoDB)
                var totalConfiguredNodes = regions.Sum(r => r.Servers.Count);
                var totalBackendConfigured = regions.Sum(r => r.Servers.Count(s => s.Type == ServerTypeEnum.Backend));
                var totalProxyConfigured = regions.Sum(r => r.Servers.Count(s => s.Type == ServerTypeEnum.Proxy));

                // Active (Redis)
                var activeBackendLive = liveNodes.Count(n => n.Type == AppNodeTypeEnum.Backend);
                var activeProxyLive = liveNodes.Count(n => n.Type == AppNodeTypeEnum.Proxy);

                // 3. Build Model Root
                var model = new InfrastructureOverviewModel
                {
                    TotalRegions = regions.Count,
                    ConfiguredNodesCount = totalConfiguredNodes,
                    ActiveNodesCount = (activeBackendLive + activeProxyLive),

                    ActiveBackendNodes = activeBackendLive,
                    TotalBackendNodes = totalBackendConfigured,

                    ActiveProxyNodes = activeProxyLive,
                    TotalProxyNodes = totalProxyConfigured
                };

                // 4. Process Singleton Nodes (Frontend/Background)
                var frontend = liveNodes.FirstOrDefault(n => n.Type == AppNodeTypeEnum.Frontend);
                if (frontend != null)
                {
                    model.FrontendNode = new SingletonNodeStatus
                    {
                        IsOnline = true,
                        CpuUsage = frontend.CpuUsagePercent,
                        RamUsage = frontend.MemoryUsagePercent,
                        Version = frontend.Version,
                        LastHeartbeat = frontend.LastUpdated
                    };
                }
                else
                {
                    // Fallback to show it exists but offline (or check config if you track singleton config)
                    model.FrontendNode = new SingletonNodeStatus { IsOnline = false };
                }

                var background = liveNodes.FirstOrDefault(n => n.Type == AppNodeTypeEnum.Background);
                if (background != null)
                {
                    model.BackgroundNode = new SingletonNodeStatus
                    {
                        IsOnline = true,
                        CpuUsage = background.CpuUsagePercent,
                        RamUsage = background.MemoryUsagePercent,
                        Version = background.Version,
                        LastHeartbeat = background.LastUpdated
                    };
                }
                else
                {
                    model.BackgroundNode = new SingletonNodeStatus { IsOnline = false };
                }

                // 5. Process Regions Summary
                foreach (var region in regions)
                {
                    // Filter live nodes belonging to this region
                    var regionNodes = liveNodes.Where(n =>
                        (n is BackendServerStatusData b && b.RegionId == region.RegionId) ||
                        (n is ProxyServerStatusData p && p.RegionId == region.RegionId)
                    ).ToList();

                    var summary = new RegionSummaryModel
                    {
                        RegionId = region.RegionId,
                        CountryCode = region.CountryCode,

                        // Maintenance/Disabled
                        MaintenanceModeEnabledAt = region.MaintenanceEnabledAt,
                        DisabledAt = region.DisabledAt,

                        // Backend Counts
                        TotalBackendNodes = region.Servers.Count(s => s.Type == ServerTypeEnum.Backend),
                        OnlineBackendNodes = regionNodes.Count(n => n.Type == AppNodeTypeEnum.Backend),

                        // Proxy Counts
                        TotalProxyNodes = region.Servers.Count(s => s.Type == ServerTypeEnum.Proxy),
                        OnlineProxyNodes = regionNodes.Count(n => n.Type == AppNodeTypeEnum.Proxy)
                    };

                    // Aggregate Metrics
                    // Backend Stats
                    summary.TotalActiveTelephonySessions = regionNodes.OfType<BackendServerStatusData>().Sum(b => b.CurrentActiveTelephonySessionCount);
                    summary.TotalActiveWebSessions = regionNodes.OfType<BackendServerStatusData>().Sum(b => b.CurrentActiveWebSessionCount);

                    // Proxy Stats
                    summary.TotalOutboundMarkedQueues = regionNodes.OfType<ProxyServerStatusData>().Sum(p => p.CurrentOutboundMarkedQueues);
                    summary.TotalOutboundProcessingQueues = regionNodes.OfType<ProxyServerStatusData>().Sum(p => p.CurrentOutboundProcessingMarkedQueues);
                    summary.TotalOutboundCompletedQueues = regionNodes.OfType<ProxyServerStatusData>().Sum(p => p.CurrentOutboundProcessedMarkedQueues);

                    model.Regions.Add(summary);
                }

                // Aggregate Global Metrics from Region Totals (more accurate than raw node sum if nodes have region context issues)
                model.TotalActiveTelephonySessions = model.Regions.Sum(r => r.TotalActiveTelephonySessions);
                model.TotalActiveWebSessions = model.Regions.Sum(r => r.TotalActiveWebSessions);
                model.TotalOutboundMarkedQueues = model.Regions.Sum(r => r.TotalOutboundMarkedQueues);
                model.TotalOutboundProcessingQueues = model.Regions.Sum(r => r.TotalOutboundProcessingQueues);
                model.TotalOutboundCompletedQueues = model.Regions.Sum(r => r.TotalOutboundCompletedQueues);

                return result.SetSuccessResult(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building infrastructure overview");
                return result.SetFailureResult("GetOverview:EXCEPTION", ex.Message);
            }
        }

        public async Task<FunctionReturnResult<RegionDetailModel>> GetRegionDetailAsync(string regionId)
        {
            var result = new FunctionReturnResult<RegionDetailModel>();
            try
            {
                // 1. Fetch Config
                var region = await _regionManager.GetRegionById(regionId);
                if (region == null) return result.SetFailureResult("NOT_FOUND", "Region not found");

                // 2. Fetch Live State (Map for O(1) lookup)
                var liveNodesMap = await _metricsManager.GetAllActiveNodesMapAsync();

                // 3. Build Model
                var model = new RegionDetailModel
                {
                    RegionId = region.RegionId,
                    CountryCode = region.CountryCode,
                    MaintenanceEnabledAt = region.MaintenanceEnabledAt,
                    PrivateMaintenanceEnabledReason = region.PrivateMaintenanceEnabledReason,
                    PublicMaintenanceEnabledReason = region.PublicMaintenanceEnabledReason,
                    DisabledAt = region.DisabledAt,
                    PrivateDisabledReason = region.PrivateDisabledReason,
                    PublicDisabledReason = region.PublicDisabledReason,
                    S3Config = region.S3Server
                };

                // 4. Map Servers (Configured)
                foreach (var serverConfig in region.Servers)
                {
                    var vm = new ServerViewModel
                    {
                        Id = serverConfig.Id,
                        Endpoint = serverConfig.Endpoint,
                        SIPPort = serverConfig.SIPPort,
                        APIKey = serverConfig.APIKey,
                        Type = serverConfig.Type,
                        IsDevelopmentServer = serverConfig.IsDevelopmentServer,
                        // Pass maintenance flags
                        MaintenanceEnabledAt = serverConfig.MaintenanceEnabledAt,
                        PrivateMaintenanceEnabledReason = serverConfig.PrivateMaintenanceEnabledReason,
                        PublicMaintenanceEnabledReason = serverConfig.PublicMaintenanceEnabledReason,
                        DisabledAt = serverConfig.DisabledAt,
                        PrivateDisabledReason = serverConfig.PrivateDisabledReason,
                        PublicDisabledReason = serverConfig.PublicDisabledReason
                    };

                    // Try Find Metrics
                    if (liveNodesMap.TryGetValue(serverConfig.Endpoint, out var metrics))
                    {
                        vm.Metrics = metrics;
                    }

                    model.Servers.Add(vm);
                }

                // 5. Detect Zombies (In Redis, Not in Config)
                var zombies = liveNodesMap.Values.Where(n =>
                    (n is BackendServerStatusData b && b.RegionId == regionId) ||
                    (n is ProxyServerStatusData p && p.RegionId == regionId)
                ).Where(n => !region.Servers.Any(s => s.Endpoint == n.NodeId));

                foreach (var z in zombies)
                {
                    model.Servers.Add(new ServerViewModel
                    {
                        Id = z.NodeId,
                        Endpoint = z.NodeId,
                        Type = z.Type == AppNodeTypeEnum.Backend ? ServerTypeEnum.Backend : ServerTypeEnum.Proxy,
                        Metrics = z
                    });
                }

                // 6. Aggregates (Recalculate based on merged view model)
                // Backend
                var backendServers = model.Servers.Where(s => s.Type == ServerTypeEnum.Backend).ToList();
                model.OnlineBackendCount = backendServers.Count(s => s.Metrics != null && s.Metrics.RuntimeStatus == NodeRuntimeStatus.Running);
                model.TotalBackendNodes = backendServers.Count();

                var backendMetrics = backendServers.Select(s => s.Metrics).OfType<BackendServerStatusData>().ToList();
                model.TotalActiveTelephonySessions = backendMetrics.Sum(m => m.CurrentActiveTelephonySessionCount);
                model.TotalActiveWebSessions = backendMetrics.Sum(m => m.CurrentActiveWebSessionCount);

                // Proxy
                var proxyServers = model.Servers.Where(s => s.Type == ServerTypeEnum.Proxy).ToList();
                model.OnlineProxyCount = proxyServers.Count(s => s.Metrics != null && s.Metrics.RuntimeStatus == NodeRuntimeStatus.Running);
                model.TotalProxyNodes = proxyServers.Count();

                var proxyMetrics = proxyServers.Select(s => s.Metrics).OfType<ProxyServerStatusData>().ToList();
                model.TotalOutboundMarkedQueues = proxyMetrics.Sum(m => m.CurrentOutboundMarkedQueues);
                model.TotalOutboundProcessingQueues = proxyMetrics.Sum(m => m.CurrentOutboundProcessingMarkedQueues);
                model.TotalOutboundCompletedQueues = proxyMetrics.Sum(m => m.CurrentOutboundProcessedMarkedQueues);

                return result.SetSuccessResult(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting region details");
                return result.SetFailureResult("GetRegionDetail:EXCEPTION", ex.Message);
            }
        }

        public async Task<FunctionReturnResult<List<ServerStatusData>>> GetServerHistoryAsync(string nodeId, DateTime startUtc, DateTime endUtc)
        {
            try
            {
                // Sanity check: prevent fetching years of data by accident
                if ((endUtc - startUtc).TotalDays > 31)
                {
                    startUtc = endUtc.AddDays(-31);
                }

                // Use the Raw method we defined previously in the Repository
                var data = await _historyRepo.GetRawServerHistoryAsync(nodeId, startUtc, endUtc);

                return new FunctionReturnResult<List<ServerStatusData>>().SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                return new FunctionReturnResult<List<ServerStatusData>>().SetFailureResult("EXCEPTION", ex.Message);
            }
        }
    }
}
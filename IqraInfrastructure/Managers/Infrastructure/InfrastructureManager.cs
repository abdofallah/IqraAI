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
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Server;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Infrastructure
{
    public class InfrastructureManager
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppRepository _appRepo;
        private readonly RegionManager _regionManager;
        private readonly ServerMetricsManager _metricsManager;
        private readonly ServerStatusRepository _historyRepo;
        private readonly ILogger<InfrastructureManager> _logger;

        public InfrastructureManager(
            IHttpClientFactory httpClientFactory,
            AppRepository appRepo,
            RegionManager regionManager,
            ServerMetricsManager metricsManager,
            ServerStatusRepository historyRepo,
            ILogger<InfrastructureManager> logger
        ) {
            _httpClientFactory = httpClientFactory;
            _appRepo = appRepo;
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
                    model.BackgroundNode = new BackgroundNodeStatus
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
                    model.BackgroundNode = new BackgroundNodeStatus { IsOnline = false };
                }
                var coreNodesConfig = await _appRepo.GetCoreNodesConfig();
                if (coreNodesConfig != null)
                {
                    model.BackgroundNode.Endpoint = coreNodesConfig.BackgroundNodeEndpoint;
                    model.BackgroundNode.UseSSL = coreNodesConfig.BackgroundNodeUseSSL;
                    model.BackgroundNode.ApiKey = coreNodesConfig.BackgroundNodeApiKey;
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
                return result.SetFailureResult(
                    "GetOverview:EXCEPTION",
                    $"Error getting overview: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<RegionDetailModel>> GetRegionDetailAsync(string regionId)
        {
            var result = new FunctionReturnResult<RegionDetailModel>();
            try
            {
                // 1. Fetch Config
                var region = await _regionManager.GetRegionById(regionId);
                if (region == null)
                {
                    return result.SetFailureResult(
                        "GetRegionDetail:NOT_FOUND",
                        "Region not found"
                    );
                }

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
                    if (liveNodesMap.TryGetValue(serverConfig.Id, out var metrics))
                    {
                        vm.Metrics = metrics;
                    }

                    model.Servers.Add(vm);
                }

                // 5. Detect Zombies (In Redis, Not in Config)
                var zombies = liveNodesMap.Values.Where(n =>
                    (n is BackendServerStatusData b && b.RegionId == regionId) ||
                    (n is ProxyServerStatusData p && p.RegionId == regionId)
                ).Where(n => !region.Servers.Any(s => s.Id == n.NodeId));

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
                return result.SetFailureResult(
                    "GetRegionDetail:EXCEPTION",
                    $"Error getting region details: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<List<ServerStatusData>?>> GetServerHistoryAsync(string nodeId, DateTime startUtc, DateTime endUtc)
        {
            var result = new FunctionReturnResult<List<ServerStatusData>?>();

            try
            {
                // Sanity check: prevent fetching years of data by accident
                if ((endUtc - startUtc).TotalDays > 31)
                {
                    startUtc = endUtc.AddDays(-31);
                }

                // Use the Raw method we defined previously in the Repository
                var data = await _historyRepo.GetRawServerHistoryAsync(nodeId, startUtc, endUtc);
                if (data == null)
                {
                    return result.SetFailureResult(
                        "GetServerHistoryAsync:NOT_FOUND",
                        "No data found"
                    );
                }

                return result.SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetServerHistoryAsync:EXCEPTION",
                    $"Error getting server history: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> ShutdownCoreBackgroundAsync()
        {
            var result = new FunctionReturnResult();

            try
            {
                var backgroundNodeRunning = await _metricsManager.CheckAnyBackgroundNodeRunning();
                if (!backgroundNodeRunning)
                {
                    return result.SetFailureResult(
                        "ShutdownCoreBackgroundAsync:NOT_ONLINE",
                        "Background node is already offline"
                    );
                }

                var coreNodesConfig = await _appRepo.GetCoreNodesConfig();
                if (coreNodesConfig == null)
                {
                    return result.SetFailureResult(
                        "ShutdownCoreBackgroundAsync:NOT_FOUND",
                        "No core nodes configuration found"
                    );
                }

                if (string.IsNullOrEmpty(coreNodesConfig.BackgroundNodeEndpoint) || string.IsNullOrEmpty(coreNodesConfig.BackgroundNodeApiKey))
                {
                    return result.SetFailureResult(
                        "ShutdownCoreBackgroundAsync:NODE_CONFIG_NOT_FOUND",
                        "Background node endpoint or api key not found in configuration found"
                    );
                }

                var shutdownRequest = await ForwardShutdownRequestToNode(coreNodesConfig.BackgroundNodeEndpoint, coreNodesConfig.BackgroundNodeUseSSL, coreNodesConfig.BackgroundNodeApiKey);
                if (!shutdownRequest.Success)
                {
                    return result.SetFailureResult(
                        $"ShutdownCoreBackgroundAsync:{shutdownRequest.Code}",
                        shutdownRequest.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "ShutdownCoreBackgroundAsync:EXCEPTION",
                    $"Error shutting down core background: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> ShutdownRegionAsync(string regionId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionAsync:NOT_FOUND",
                        "Region not found"
                    );
                }

                if (regionData.Servers.Count == 0)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionAsync:NOT_FOUND",
                        "No servers found in region"
                    );
                }

                var liveRegionNodes = await _metricsManager.GetAllActiveNodesAsync();
                if (liveRegionNodes.Count == 0)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionAsync:NOT_FOUND",
                        "No live nodes found in region"
                    );
                }

                var nodeIds = new List<string>();
                var shutdownRequestTasks = new List<Task<FunctionReturnResult>>();
                foreach (var server in regionData.Servers)
                {
                    var isNodeLive = false;

                    foreach (var livenode in liveRegionNodes)
                    {
                        if (isNodeLive) break;

                        if (server.Type == ServerTypeEnum.Backend)
                        {
                            if (livenode is BackendServerStatusData bsd)
                            {
                                if (bsd.RegionId == regionId)
                                {
                                    if (bsd.RuntimeStatus != NodeRuntimeStatus.Draining)
                                    {
                                        isNodeLive = true;
                                    }
                                }
                            }
                        }
                        else if (server.Type == ServerTypeEnum.Proxy)
                        {
                            if (livenode is ProxyServerStatusData psd)
                            {
                                if (psd.RegionId == regionId)
                                {
                                    if (psd.RuntimeStatus != NodeRuntimeStatus.Draining)
                                    {
                                        isNodeLive = true;
                                    }
                                }
                            }
                        }
                    }

                    if (isNodeLive)
                    {
                        nodeIds.Add(server.Id);
                        shutdownRequestTasks.Add(ForwardShutdownRequestToNode(server.Endpoint, server.UseSSL, server.APIKey, true));
                    }
                }

                if (shutdownRequestTasks.Count == 0)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionAsync:NOT_FOUND",
                        "No live nodes found in region"
                    );
                }

                await Task.WhenAll(shutdownRequestTasks);

                var shutdownRequestResult = shutdownRequestTasks.Select(t => t.Result).ToList();

                var failedShutdownRequests = new List<string>();
                for (var i = 0; i < shutdownRequestResult.Count; i++)
                {
                    if (!shutdownRequestResult[i].Success)
                    {
                        failedShutdownRequests.Add(nodeIds[i]);
                    }
                }

                if (failedShutdownRequests.Count > 0)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionAsync:EXCEPTION",
                        $"Error shutting down region servers: [{string.Join(", ", failedShutdownRequests)}], Other servers if any were requested to shut down successfully."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ShutdownRegionAsync:EXCEPTION",
                    $"Error shutting down region: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> ShutdownRegionServerAsync(string regionId, string serverId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var regionData = await _regionManager.GetRegionById(regionId);
                if (regionData == null)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionServerAsync:NOT_FOUND",
                        "Region not found"
                    );
                }

                var serverData = regionData.Servers.FirstOrDefault(s => s.Id == serverId);
                if (serverData == null)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionServerAsync:NOT_FOUND",
                        "Server not found in region"
                    );
                }

                var liveMetricData = await _metricsManager.GetServerStatusData(regionId, serverId);
                if (liveMetricData == null)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionServerAsync:SERVER_OFFLINE",
                        "Server is offline"
                    );
                }

                if (liveMetricData.RuntimeStatus == NodeRuntimeStatus.Draining)
                {
                    return result.SetFailureResult(
                        "ShutdownRegionServerAsync:SERVER_OFFLINE",
                        "Server is already shutting down"
                    );
                }

                var shutdownRequest = await ForwardShutdownRequestToNode(serverData.Endpoint, serverData.UseSSL, serverData.APIKey);
                if (!shutdownRequest.Success)
                {
                    return result.SetFailureResult(
                        $"ShutdownRegionServerAsync:{shutdownRequest.Code}",
                        shutdownRequest.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ShutdownRegionServerAsync:EXCEPTION",
                    $"Error shutting down region server: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> UpdateCoreBackgroundConfigAsync(UpdateCoreBackgroundConfigRequestModel data)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (string.IsNullOrEmpty(data.Endpoint))
                {
                    return result.SetFailureResult(
                        "UpdateCoreBackgroundConfigAsync:NOT_FOUND",
                        "Endpoint not found or empty"
                    );
                }

                if (string.IsNullOrEmpty(data.ApiKey))
                {
                    return result.SetFailureResult(
                        "UpdateCoreBackgroundConfigAsync:NOT_FOUND",
                        "API Key not found or empty"
                    );
                }
                else if (data.ApiKey.Length < 32)
                {
                    return result.SetFailureResult(
                        "UpdateCoreBackgroundConfigAsync:API_KEY_TOO_SHORT",
                        "API Key can not be smaller than 32 chars"
                    );
                }

                var updateResult = await _appRepo.AddUpdateCoreNodeBackgroundNodeConfig(data.Endpoint, data.UseSSL, data.ApiKey);
                if (!updateResult)
                {
                    return result.SetFailureResult(
                        "UpdateCoreBackgroundConfigAsync:EXCEPTION",
                        "Error updating core background config"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdateCoreBackgroundConfigAsync:EXCEPTION",
                    $"Error updating core background config: {ex.Message}"
                );
            }
        }

        private async Task<FunctionReturnResult> ForwardShutdownRequestToNode(string endpoint, bool useSSL, string apiKey, bool ignoreAlreadyShuttingDown = false)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient();

                // Set headers
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                // Prepare the request body
                var request = new
                {
                    IgnoreAlreadyShuttingDown = ignoreAlreadyShuttingDown
                };
                var content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");

                // Send the notification
                string serverEndpoint = endpoint;
                if (useSSL)
                {
                    serverEndpoint = "https://" + serverEndpoint;
                }
                else
                {
                    serverEndpoint = "http://" + serverEndpoint;
                }

                var baseUri = new Uri(serverEndpoint);
                baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/node/management/shutdown");
                var response = await client.PostAsync(baseUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return result.SetFailureResult(
                        $"ForwardShutdownRequestToNode:STATUS_CODE_{response.StatusCode}",
                        errorContent
                    );
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                FunctionReturnResult? responseData = null;
                try
                {
                    responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent);
                }
                catch { /** do nothing **/ }
                if (responseData == null) // should never hapopen tho
                {
                    return result.SetFailureResult(
                        "ForwardShutdownRequestToNode:INVALID_RESPONSE",
                        responseContent
                    );
                }

                if (!responseData.Success)
                {
                    return result.SetFailureResult(
                        $"ForwardShutdownRequestToNode:{responseData.Code}",
                        responseData.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ForwardShutdownRequestToNode:EXCEPTION",
                    $"Error forwarding shutdown request to node: {ex.Message}"
                );
            }
        }
    }
}
using IqraCore.Entities.App.Enum;
using IqraCore.Entities.App.Lifecycle;
using IqraCore.Entities.Node.Enum;
using IqraCore.Interfaces.Node;
using IqraInfrastructure.Managers.App;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Region;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Node
{
    public class NodeLifecycleManager
    {
        private readonly AppNodeTypeEnum _appNodeType;
        private readonly IHostApplicationLifetime _hostLifetime;
        private readonly IqraAppManager _appManager;
        private readonly AppRepository _appRepository;
        private readonly RegionRepository _regionRepository;
        private readonly ILogger<NodeLifecycleManager> _logger;

        private readonly INodeWorkloadMonitor? _workloadMonitor;
        private NodeRuntimeStatus _currentStatus = NodeRuntimeStatus.Starting;
        private string _currentStatusReason = "Node Starting!";

        // Identity
        private string? _regionId;
        private string? _nodeId;
        private bool _isIdentitySet = false;

        private bool _isShutdownRequested = false;

        public NodeLifecycleManager(
            AppNodeTypeEnum appNodeType,
            IHostApplicationLifetime hostLifetime,
            IqraAppManager appManager,
            AppRepository appRepository,
            RegionRepository regionRepository,
            INodeWorkloadMonitor? workloadMonitor,
            ILogger<NodeLifecycleManager> logger
        ) {
            _appNodeType = appNodeType;
            _hostLifetime = hostLifetime;
            _appManager = appManager;
            _appRepository = appRepository;
            _regionRepository = regionRepository;
            _workloadMonitor = workloadMonitor;
            _logger = logger;
        }

        public NodeRuntimeStatus Status => _currentStatus;
        public string StatusReason => _currentStatusReason;
        public bool IsAcceptingNewWork => _currentStatus == NodeRuntimeStatus.Running;
        public bool IsShutdownRequested => _isShutdownRequested;

        public void SetIdentity(string regionId, string nodeId)
        {
            _regionId = regionId;
            _nodeId = nodeId;
            _isIdentitySet = true;
            _currentStatus = NodeRuntimeStatus.Running;
        }
        public void SetShutdownRequested(bool isShutdownRequested)
        {
            _isShutdownRequested = isShutdownRequested;
        }

        public async Task EvaluateStateAsync()
        {
            // Default assumes we want to run, unless a check says otherwise
            var targetStatus = NodeRuntimeStatus.Running;
            var statusReason = "Normal Operation";

            // Check shutdown requested
            if (_isShutdownRequested)
            {
                targetStatus = NodeRuntimeStatus.Draining;
                statusReason = "Shutdown Requested";
            }
            else if (_appManager.CurrentStatus == AppLifecycleStatus.VersionMismatch)
            {
                targetStatus = NodeRuntimeStatus.Draining;
                statusReason = "Global Version Mismatch (Obsolete Binary)";
            }
            else if (_appManager.CurrentConfig?.IsMigrationInProgress == true)
            {
                targetStatus = NodeRuntimeStatus.Draining;
                statusReason = "Cluster Migration In Progress";
            }

            if (targetStatus != NodeRuntimeStatus.Draining)
            {
                // Check global maintenance
                var appPermissionConfig = await _appRepository.GetAppPermissionConfig();
                if (appPermissionConfig?.MaintenanceEnabledAt != null)
                {
                    targetStatus = NodeRuntimeStatus.Maintenance;
                    statusReason = "Global Maintenance Mode Enabled";
                    if (!string.IsNullOrWhiteSpace(appPermissionConfig?.PublicMaintenanceEnabledReason))
                    {
                        statusReason += ": " + appPermissionConfig.PublicMaintenanceEnabledReason;
                    }
                }

                // Node Specific Checks (Backend/Proxy)
                if (
                    (_appNodeType == AppNodeTypeEnum.Backend || _appNodeType == AppNodeTypeEnum.Proxy) &&
                    _isIdentitySet && !string.IsNullOrEmpty(_regionId) && !string.IsNullOrEmpty(_nodeId)
                )
                {
                    var regionData = await _regionRepository.GetRegionById(_regionId);
                    if (regionData != null)
                    {
                        // Region Disabled -> Drain
                        if (regionData.DisabledAt != null)
                        {
                            targetStatus = NodeRuntimeStatus.Draining;
                            statusReason = "Region Disabled by Admin";
                            if (!string.IsNullOrWhiteSpace(regionData.PublicDisabledReason))
                            {
                                statusReason += ": " + regionData.PublicDisabledReason;
                            }
                        }
                        // Region Maintenance -> Maintenance (Only if not already draining)
                        else if (regionData.MaintenanceEnabledAt != null && targetStatus != NodeRuntimeStatus.Draining)
                        {
                            targetStatus = NodeRuntimeStatus.Maintenance;
                            statusReason = "Region Maintenance Mode Enabled";
                            if (!string.IsNullOrWhiteSpace(regionData.PublicMaintenanceEnabledReason))
                            {
                                statusReason += ": " + regionData.PublicMaintenanceEnabledReason;
                            }
                        }
                        else
                        {
                            var serverData = regionData.Servers.FirstOrDefault(s => s.Endpoint == _nodeId || s.Id == _nodeId);
                            if (serverData != null)
                            {
                                // Server Disabled -> Drain
                                if (serverData.DisabledAt != null)
                                {
                                    targetStatus = NodeRuntimeStatus.Draining;
                                    statusReason = "Node Disabled by Admin";
                                    if (!string.IsNullOrWhiteSpace(serverData.PublicDisabledReason))
                                    {
                                        statusReason += ": " + serverData.PublicDisabledReason;
                                    }
                                }
                                // Server Maintenance -> Maintenance (Only if not already draining)
                                else if (serverData.MaintenanceEnabledAt != null && targetStatus != NodeRuntimeStatus.Draining)
                                {
                                    targetStatus = NodeRuntimeStatus.Maintenance;
                                    statusReason = "Node Maintenance Mode Enabled";
                                    if (!string.IsNullOrWhiteSpace(serverData.PublicMaintenanceEnabledReason))
                                    {
                                        statusReason += ": " + serverData.PublicMaintenanceEnabledReason;
                                    }
                                }
                            }
                            else
                            {
                                // Server Config Missing -> Drain (Zombie Protection)
                                targetStatus = NodeRuntimeStatus.Draining;
                                statusReason = "Node Configuration Not Found in Database";
                            }
                        }
                    }
                    else
                    {
                        // Region Config Missing -> Drain
                        targetStatus = NodeRuntimeStatus.Draining;
                        statusReason = "Region Configuration Not Found in Database";
                    }
                }
            }

            // If all checks passed and target is Running, this will recover the node from Maintenance.
            TransitionTo(targetStatus, statusReason);

            // Handle Draining State (The Reaper)
            if (_currentStatus == NodeRuntimeStatus.Draining)
            {
                int totalActiveWork = await (_workloadMonitor?.GetActiveWorkloadCountAsync() ?? Task.FromResult(0));

                if (totalActiveWork == 0)
                {
                    _logger.LogWarning("Node Drained successfully (0 active tasks). Initiating System Shutdown.");
                    _hostLifetime.StopApplication();
                }
                else
                {
                    _logger.LogInformation("Node Draining... Waiting for {Count} active tasks to finish.", totalActiveWork);
                }
            }
        }

        private void TransitionTo(NodeRuntimeStatus newStatus, string reason)
        {
            if (_currentStatus == newStatus && _currentStatusReason == reason) return;

            // One-Way Ticket: Once Draining, you can only go to Stopped.
            // You cannot recover to Running/Maintenance from Draining without a restart 
            // (unless Shutdown Request is manually cancelled, but usually Draining implies process death is imminent).
            if (_currentStatus == NodeRuntimeStatus.Draining && newStatus != NodeRuntimeStatus.Stopped)
            {
                return;
            }

            _logger.LogWarning("Node State Transition: {Old} -> {New}. Reason: {Reason}", _currentStatus, newStatus, reason);
            _currentStatus = newStatus;
            _currentStatusReason = reason;
        }
    }
}
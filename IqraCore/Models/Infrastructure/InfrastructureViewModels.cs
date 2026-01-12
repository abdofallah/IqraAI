using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;

namespace IqraCore.Models.Infrastructure
{
    // --- Dashboard Overview ---
    public class InfrastructureOverviewModel
    {
        // Global Aggregates
        public int TotalRegions { get; set; }
        // Backend
        public int TotalActiveWebSessions { get; set; }
        public int TotalActiveTelephonySessions { get; set; }
        // Proxy
        public int TotalOutboundMarkedQueues { get; set; }
        public int TotalOutboundProcessingQueues { get; set; }
        public int TotalOutboundCompletedQueues { get; set; }

        // Node Counts (Live / Configured)
        public int ActiveNodesCount { get; set; }
        public int ConfiguredNodesCount { get; set; }

        // Specific Node Counts (Active / Configured)
        public int ActiveBackendNodes { get; set; }
        public int TotalBackendNodes { get; set; }

        public int ActiveProxyNodes { get; set; }
        public int TotalProxyNodes { get; set; }

        // Core Services
        public SingletonNodeStatus? FrontendNode { get; set; }
        public SingletonNodeStatus? BackgroundNode { get; set; }

        public List<RegionSummaryModel> Regions { get; set; } = new();
    }

    public class SingletonNodeStatus
    {
        public bool IsOnline { get; set; }
        public double CpuUsage { get; set; }
        public double RamUsage { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; }
    }

    // --- Region Summary Card ---
    public class RegionSummaryModel
    {
        public string RegionId { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        // Configuration
        public int ConfiguredServers { get; set; }
        
        public DateTime? MaintenanceModeEnabledAt { get; set; }     
        public DateTime? DisabledAt { get; set; }

        // Live State
        // Backend
        public int OnlineBackendNodes { get; set; }
        public int TotalBackendNodes { get; set; }
        public int TotalActiveWebSessions { get; set; }
        public int TotalActiveTelephonySessions { get; set; }
        // Proxy
        public int OnlineProxyNodes { get; set; }
        public int TotalProxyNodes { get; set; }
        public int TotalOutboundMarkedQueues { get; set; }
        public int TotalOutboundProcessingQueues { get; set; }
        public int TotalOutboundCompletedQueues { get; set; }
    }

    // --- Region Detail View ---
    public class RegionDetailModel
    {
        // Flattened Config
        public string RegionId { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;

        // Flattened Maintenance/Disabled Config
        public DateTime? MaintenanceEnabledAt { get; set; }
        public string? PrivateMaintenanceEnabledReason { get; set; }
        public string? PublicMaintenanceEnabledReason { get; set; }

        public DateTime? DisabledAt { get; set; }
        public string? PrivateDisabledReason { get; set; }
        public string? PublicDisabledReason { get; set; }

        // Configuration
        public RegionS3StorageServerData S3Config { get; set; } = new();

        // Live Aggregates
        // Backend
        public int OnlineBackendCount { get; set; }
        public int TotalBackendNodes { get; set; }
        public int TotalActiveTelephonySessions { get; set; }
        public int TotalActiveWebSessions { get; set; }
        // Proxy
        public int OnlineProxyCount { get; set; }
        public int TotalProxyNodes { get; set; }
        public int TotalOutboundMarkedQueues { get; set; }
        public int TotalOutboundProcessingQueues { get; set; }
        public int TotalOutboundCompletedQueues { get; set; }

        // Child Components
        public List<ServerViewModel> Servers { get; set; } = new();
    }

    // --- Server Detail View ---
    public class ServerViewModel
    {
        // Identity (From MongoDB)
        public string Id { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public int SIPPort { get; set; }
        public string APIKey { get; set; } = string.Empty;
        public ServerTypeEnum Type { get; set; }
        public bool IsDevelopmentServer { get; set; }

        // Maintenance/Disabled (From MongoDB)
        public DateTime? MaintenanceEnabledAt { get; set; }
        public string? PrivateMaintenanceEnabledReason { get; set; }
        public string? PublicMaintenanceEnabledReason { get; set; }

        public DateTime? DisabledAt { get; set; }
        public string? PrivateDisabledReason { get; set; }
        public string? PublicDisabledReason { get; set; }

        // Runtime State (From Redis)
        public ServerStatusData? Metrics { get; set; }
    }
}
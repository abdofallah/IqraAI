using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server.Metrics;
using IqraCore.Models.Infrastructure;
using IqraInfrastructure.Managers.Server.Metrics;
using IqraInfrastructure.Repositories.Region;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace IqraInfrastructure.Managers.Region
{
    public class RegionManager
    {
        private readonly RegionRepository _regionRepository;
        private ServerMetricsManager? _serverMetricsManager;
        private ILogger<RegionManager>? _logger;

        public RegionManager(
            RegionRepository regionRepository
        ) {
            _regionRepository = regionRepository;
        }

        public void SetLogger(ILogger<RegionManager> logger)
        {
            _logger = logger;
        }

        public void SetDependencies(ServerMetricsManager serverMetricsManager)
        {
            _serverMetricsManager = serverMetricsManager;
        }

        // --- Reads ---
        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions()
        {
            var result = new FunctionReturnResult<List<RegionData>?>();
            var regions = await _regionRepository.GetRegions();

            if (regions == null)
            {
                return result.SetFailureResult(
                    "GetRegions:NOT_FOUND",
                    "Regions not found"
                );
            }

            return result.SetSuccessResult(regions);
        }

        public async Task<RegionData?> GetRegionById(string regionId)
        {
            return await _regionRepository.GetRegionById(regionId);
        }

        public async Task<bool> CheckRegionExists(string regionId)
        {
            return await _regionRepository.CheckRegionExists(regionId);
        }

        // --- Region Write Operations ---

        public async Task<FunctionReturnResult> CreateRegion(string countryCode, string regionName)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(regionName))
                {
                    return result.SetFailureResult("CreateRegion:INVALID_INPUT", "Country Code and Region Name are required.");
                }

                var resultingId = $"{countryCode.Trim()}-{regionName.Trim()}".ToUpper();

                var exists = await _regionRepository.CheckRegionExists(resultingId);
                if (exists)
                {
                    return result.SetFailureResult("CreateRegion:ALREADY_EXISTS", $"Region '{resultingId}' already exists.");
                }

                var newRegion = new RegionData
                {
                    RegionId = resultingId,
                    CountryCode = countryCode.ToUpper(),
                    RegionName = regionName.ToUpper(),
                    // Default to Disabled & Maintenance Mode for safety upon creation
                    DisabledAt = DateTime.UtcNow,
                    PublicDisabledReason = "Region initialization",
                    PrivateDisabledReason = "Newly created region",
                    MaintenanceEnabledAt = DateTime.UtcNow,
                    PublicMaintenanceEnabledReason = "Region initialization",
                    PrivateMaintenanceEnabledReason = "Newly created region"
                };

                var success = await _regionRepository.AddRegion(newRegion);
                if (!success)
                {
                    return result.SetFailureResult(
                        "CreateRegion:DB_ERROR",
                        "Failed to insert region record."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating region");
                return result.SetFailureResult(
                    "CreateRegion:EXCEPTION",
                    $"Error creating region: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> DeleteRegionSafe(string regionId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var region = await _regionRepository.GetRegionById(regionId);
                if (region == null)
                {
                    return result.SetFailureResult(
                        "DeleteRegion:NOT_FOUND",
                        "Region not found."
                    );
                }

                // Check Must be Disabled
                if (region.DisabledAt == null)
                {
                    return result.SetFailureResult(
                        "DeleteRegion:NOT_DISABLED",
                        "Region must be fully Disabled before deletion."
                    );
                }

                // Check No Live Nodes
                if (_serverMetricsManager == null)
                {
                    return result.SetFailureResult(
                        "DeleteRegion:SERVER_METRICS_NOT_FOUND",
                        "Server Metrics Manager not found."
                    );
                }
                var activeNodes = await _serverMetricsManager.GetAllActiveNodesAsync();
                var liveNodesInRegion = activeNodes.Where(n =>
                    (n is BackendServerStatusData b && b.RegionId == regionId) ||
                    (n is ProxyServerStatusData p && p.RegionId == regionId)
                ).ToList();
                if (liveNodesInRegion.Any())
                {
                    return result.SetFailureResult(
                        "DeleteRegion:NODES_ONLINE",
                        $"Cannot delete. {liveNodesInRegion.Count} nodes are still reporting status."
                    );
                }

                // Delete
                var success = await _regionRepository.DeleteRegion(regionId);
                if (!success)
                {
                    return result.SetFailureResult(
                        "DeleteRegion:DB_ERROR",
                        "Failed to delete region document."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting region");
                return result.SetFailureResult("DeleteRegion:EXCEPTION", ex.Message);
            }
        }

        public async Task<FunctionReturnResult> UpdateRegionS3Config(string regionId, RegionS3StorageServerData s3Data)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Basic Validation
                if (string.IsNullOrWhiteSpace(s3Data.Endpoint))
                {
                    return result.SetFailureResult(
                        "UpdateRegionS3:ENDPOINT_REQUIRED",
                        "S3 Endpoint is required."
                    );
                }
                if (string.IsNullOrWhiteSpace(s3Data.AccessKey))
                {
                    return result.SetFailureResult(
                        "UpdateRegionS3:ACCESS_KEY_REQUIRED",
                        "S3 Access Key is required."
                    );
                }
                if (string.IsNullOrWhiteSpace(s3Data.SecretKey))
                {
                    return result.SetFailureResult(
                        "UpdateRegionS3:SECRET_KEY_REQUIRED",
                        "S3 Secret Key is required."
                    );
                }

                // Check Region Exists
                var regionExists = await _regionRepository.CheckRegionExists(regionId);
                if (!regionExists)
                {
                    return result.SetFailureResult(
                        "UpdateRegionS3:NOT_FOUND",
                        "Region not found."
                    );
                }

                // Perform Update - do not change disabled field
                var updateDefinition = Builders<RegionData>.Update
                    .Set(x => x.S3Server.Endpoint, s3Data.Endpoint)
                    .Set(x => x.S3Server.AccessKey, s3Data.AccessKey)
                    .Set(x => x.S3Server.SecretKey, s3Data.SecretKey)
                    .Set(x => x.S3Server.UseSSL, s3Data.UseSSL);

                var success = await _regionRepository.UpdateRegion(regionId, updateDefinition);
                if (!success)
                {
                    return result.SetFailureResult(
                        "UpdateRegionS3:DB_ERROR",
                        "Failed to update S3 configuration."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating region S3 config");
                return result.SetFailureResult(
                    "UpdateRegionS3:EXCEPTION",
                    $"Error updating region S3 config: {ex.Message}"
                );
            }
        }

        // --- Server Write Operations ---

        public async Task<FunctionReturnResult> AddOrUpdateRegionServer(string postType, string regionId, string? serverId, CreateUpdateServerRequestModel inputData)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (postType != "add" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveServer:INVALID_POST_TYPE",
                        "Invalid post type. Must be 'add' or 'edit'."
                    );
                }

                var region = await _regionRepository.GetRegionById(regionId);
                if (region == null)
                {
                    return result.SetFailureResult(
                        "SaveServer:REGION_NOT_FOUND",
                        "Region not found."
                    );
                }

                // Data Validaiton
                if (string.IsNullOrEmpty(inputData.Endpoint))
                {
                    return result.SetFailureResult(
                        "SaveServer:INVALID_ENDPOINT",
                        "Server endpoint is required."
                    );
                }

                if (inputData.Type != ServerTypeEnum.Backend && inputData.Type != ServerTypeEnum.Proxy)
                {
                    return result.SetFailureResult(
                        "SaveServer:INVALID_TYPE",
                        "Server type must be 'Backend' or 'Proxy'."
                    );
                }

                if (string.IsNullOrEmpty(inputData.APIKey) || inputData.APIKey.Length < 32)
                {
                    return result.SetFailureResult(
                        "SaveServer:INVALID_API_KEY",
                        "Server API Key is required. Must be 32 characters or greater."
                    );
                }

                if (inputData.SIPPort < 1024 || inputData.SIPPort > 65535)
                {
                    return result.SetFailureResult(
                        "SaveServer:INVALID_SIP_PORT",
                        "SIP Port must be between 1024 and 65535."
                    );
                }

                if (postType == "add")
                {
                    var endpointConflictServer = region.Servers.Find(s => s.Endpoint == inputData.Endpoint);
                    if (endpointConflictServer != null)
                    {
                        return result.SetFailureResult(
                            "SaveServer:ENDPOINT_EXISTS",
                            $"Endpoint '{inputData.Endpoint}' is already in use in this region by server '{endpointConflictServer.Id}'."
                        );
                    }

                    var newServer = new RegionServerData
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Endpoint = inputData.Endpoint,
                        Type = inputData.Type,
                        APIKey = inputData.APIKey,
                        SIPPort = inputData.SIPPort,
                        IsDevelopmentServer = inputData.IsDevelopmentServer,
                        UseSSL = inputData.UseSSL,
                        // Default to Disabled
                        DisabledAt = DateTime.UtcNow,
                        PrivateDisabledReason = "New Server",
                        PublicDisabledReason = "Under Maintenance",
                        MaintenanceEnabledAt = DateTime.UtcNow,
                        PrivateMaintenanceEnabledReason = "New Server",
                        PublicMaintenanceEnabledReason = "Under Maintenance",
                    };

                    var success = await _regionRepository.AddRegionServer(regionId, newServer);
                    if (!success)
                    {
                        return result.SetFailureResult(
                            "SaveServer:ADD_FAILED",
                            "Failed to add server to region."
                        );
                    }
                }
                else if (postType == "edit")
                {
                    if (string.IsNullOrEmpty(serverId))
                    {
                        return result.SetFailureResult(
                            "SaveServer:INVALID_SERVER_ID",
                            "Server ID is required for 'edit' operation."
                        );
                    }

                    var existingServer = region.Servers.Find(s => s.Id == serverId);
                    if (existingServer == null)
                    {
                        return result.SetFailureResult(
                            "SaveServer:SERVER_NOT_FOUND",
                            "Server not found."
                        );
                    }

                    if (inputData.Type != existingServer.Type)
                    {
                        return result.SetFailureResult(
                            "SaveServer:INVALID_TYPE",
                            "Server type cannot be changed."
                        );
                    }

                    // Build Update Definition
                    var filter = Builders<RegionData>.Filter.And(
                        Builders<RegionData>.Filter.Eq(r => r.RegionId, regionId),
                        Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Id == serverId)
                    );

                    // Update ONLY config fields. Do NOT touch Status fields (Maintenance/Disabled)
                    var update = Builders<RegionData>.Update
                        .Set(r => r.Servers.FirstMatchingElement().Endpoint, inputData.Endpoint)
                        .Set(r => r.Servers.FirstMatchingElement().Type, inputData.Type)
                        .Set(r => r.Servers.FirstMatchingElement().APIKey, inputData.APIKey)
                        .Set(r => r.Servers.FirstMatchingElement().SIPPort, inputData.SIPPort)
                        .Set(r => r.Servers.FirstMatchingElement().IsDevelopmentServer, inputData.IsDevelopmentServer)
                        .Set(r => r.Servers.FirstMatchingElement().UseSSL, inputData.UseSSL);

                    var success = await _regionRepository.UpdateRegion(filter, update);
                    if (!success)
                    {
                        return result.SetFailureResult(
                            "SaveServer:UPDATE_FAILED",
                            "Failed to update server configuration."
                        );
                    }
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving region server");
                return result.SetFailureResult(
                    "SaveServer:EXCEPTION",
                    $"Error saving region server: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult> DeleteRegionServerSafe(string regionId, string serverId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var region = await _regionRepository.GetRegionById(regionId);
                if (region == null)
                {
                    return result.SetFailureResult("DeleteServer:REGION_NOT_FOUND", "Region not found.");
                }

                var server = region.Servers.FirstOrDefault(s => s.Id == serverId);
                if (server == null)
                {
                    return result.SetFailureResult(
                    "DeleteServer:SERVER_NOT_FOUND",
                    "Server not found."
                );
                }

                // Check 1: Must be Disabled
                if (server.DisabledAt == null)
                {
                    return result.SetFailureResult(
                        "DeleteServer:NOT_DISABLED",
                        "Server must be Disabled manually before deletion."
                    );
                }

                // Check 2: Is Live?
                if (_serverMetricsManager == null)
                {
                    return result.SetFailureResult(
                        "DeleteServer:SERVER_METRICS_NOT_ENABLED",
                        "Server metrics not enabled."
                    );
                }
                var nodeServerStatus = await _serverMetricsManager.GetServerStatusData(regionId, serverId);
                if (nodeServerStatus != null)
                {
                    return result.SetFailureResult(
                        "DeleteServer:NODE_ONLINE",
                        "Cannot delete. Server is currently reporting as Online/Draining/or etc."
                    );
                }

                // Delete
                var success = await _regionRepository.DeleteRegionServer(regionId, serverId);
                if (!success)
                {
                    return result.SetFailureResult(
                        "DeleteServer:DB_ERROR",
                        "Failed to remove server from configuration."
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting region server");
                return result.SetFailureResult("DeleteServer:EXCEPTION", ex.Message);
            }
        }

        // --- Status Toggles ---
        public async Task<bool> EnableRegion(string regionId)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.DisabledAt, null)
                .Set(x => x.PrivateDisabledReason, null)
                .Set(x => x.PublicDisabledReason, null);
            return await _regionRepository.UpdateRegion(regionId, updateDefinition);
        }

        public async Task<bool> DisableRegion(string regionId, string publicReason, string privateReason)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.DisabledAt, DateTime.UtcNow)
                .Set(x => x.PrivateDisabledReason, privateReason)
                .Set(x => x.PublicDisabledReason, publicReason);
            return await _regionRepository.UpdateRegion(regionId, updateDefinition);
        }

        public async Task<bool> EnableRegionMaintenance(string regionId, string publicReason, string privateReason)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.MaintenanceEnabledAt, DateTime.UtcNow)
                .Set(x => x.PublicMaintenanceEnabledReason, publicReason)
                .Set(x => x.PrivateMaintenanceEnabledReason, privateReason);
            return await _regionRepository.UpdateRegion(regionId, updateDefinition);
        }

        public async Task<bool> DisableRegionMaintenance(string regionId)
        {
            var updateDefinition = Builders<RegionData>.Update
                .Set(x => x.MaintenanceEnabledAt, null)
                .Set(x => x.PublicMaintenanceEnabledReason, null)
                .Set(x => x.PrivateMaintenanceEnabledReason, null);
            return await _regionRepository.UpdateRegion(regionId, updateDefinition);
        }

        public async Task<bool> EnableRegionServer(string regionId, string serverId)
        {
            var filter = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.RegionId, regionId),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Id == serverId)
            );
            var update = Builders<RegionData>.Update
                .Set(r => r.Servers.FirstMatchingElement().DisabledAt, null)
                .Set(r => r.Servers.FirstMatchingElement().PrivateDisabledReason, null)
                .Set(r => r.Servers.FirstMatchingElement().PublicDisabledReason, null);
            return await _regionRepository.UpdateRegion(filter, update);
        }

        public async Task<bool> DisableRegionServer(string regionId, string serverId, string publicReason, string privateReason)
        {
            var filter = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.RegionId, regionId),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Id == serverId)
            );
            var update = Builders<RegionData>.Update
                .Set(r => r.Servers.FirstMatchingElement().DisabledAt, DateTime.UtcNow)
                .Set(r => r.Servers.FirstMatchingElement().PrivateDisabledReason, privateReason)
                .Set(r => r.Servers.FirstMatchingElement().PublicDisabledReason, publicReason);
            return await _regionRepository.UpdateRegion(filter, update);
        }

        public async Task<bool> EnableRegionServerMaintenance(string regionId, string serverId, string publicReason, string privateReason)
        {
            var filter = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.RegionId, regionId),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Id == serverId)
            );
            var update = Builders<RegionData>.Update
                .Set(r => r.Servers.FirstMatchingElement().MaintenanceEnabledAt, DateTime.UtcNow)
                .Set(r => r.Servers.FirstMatchingElement().PublicMaintenanceEnabledReason, publicReason)
                .Set(r => r.Servers.FirstMatchingElement().PrivateMaintenanceEnabledReason, privateReason);
            return await _regionRepository.UpdateRegion(filter, update);
        }

        public async Task<bool> DisableRegionServerMaintenance(string regionId, string serverId)
        {
            var filter = Builders<RegionData>.Filter.And(
                Builders<RegionData>.Filter.Eq(r => r.RegionId, regionId),
                Builders<RegionData>.Filter.ElemMatch(r => r.Servers, s => s.Id == serverId)
            );
            var update = Builders<RegionData>.Update
                .Set(r => r.Servers.FirstMatchingElement().MaintenanceEnabledAt, null)
                .Set(r => r.Servers.FirstMatchingElement().PublicMaintenanceEnabledReason, null)
                .Set(r => r.Servers.FirstMatchingElement().PrivateMaintenanceEnabledReason, null);
            return await _regionRepository.UpdateRegion(filter, update);
        }
    }
}
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Entities.Server;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.Infrastructure;
using IqraInfrastructure.Managers.Infrastructure;
using IqraInfrastructure.Managers.Region;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    [Route("app/admin/infrastructure")]
    public class AppAdminInfrastructureController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper
            ;
        private readonly InfrastructureManager _infraManager;
        private readonly RegionManager _regionManager;

        public AppAdminInfrastructureController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            InfrastructureManager infraManager,
            RegionManager regionManager)
        {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
            _infraManager = infraManager;
            _regionManager = regionManager;
        }

        [HttpGet("overview")]
        public async Task<FunctionReturnResult<InfrastructureOverviewModel?>> GetOverview()
        {
            var result = new FunctionReturnResult<InfrastructureOverviewModel?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetOverview:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var overviewResult = await _infraManager.GetOverviewAsync();
                if (!overviewResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetOverview:{overviewResult.Code}",
                        overviewResult.Message
                    );
                }

                return result.SetSuccessResult(overviewResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOverview:EXCEPTION",
                    $"Failed to get infrastructure overview. Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("regions/{regionId}")]
        public async Task<FunctionReturnResult<RegionDetailModel?>> GetRegionDetail(string regionId)
        {
            var result = new FunctionReturnResult<RegionDetailModel?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetRegionDetail:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var detailResult = await _infraManager.GetRegionDetailAsync(regionId);
                if (!detailResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetRegionDetail:{detailResult.Code}",
                        detailResult.Message
                    );
                }

                return result.SetSuccessResult(detailResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetRegionDetail:EXCEPTION",
                    $"Failed to get region detail. Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("servers/{nodeId}/history")]
        public async Task<FunctionReturnResult<List<ServerStatusData>?>> GetServerHistory(
            string nodeId,
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null
        ) {
            var result = new FunctionReturnResult<List<ServerStatusData>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetServerHistory:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var endUtc = end ?? DateTime.UtcNow;
                var startUtc = start ?? endUtc.AddHours(-24);

                var historyResult = await _infraManager.GetServerHistoryAsync(nodeId, startUtc, endUtc);
                if (!historyResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetServerHistory:{historyResult.Code}",
                        historyResult.Message
                    );
                }

                return result.SetSuccessResult(historyResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetServerHistory:EXCEPTION",
                    $"Failed to get server history. Exception: {ex.Message}"
                );
            }
        }

        // REGION MANAGEMENT
        [HttpPost("regions")]
        public async Task<FunctionReturnResult> AddRegion([FromBody] CreateRegionRequestModel data)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"AddRegion:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var createResult = await _regionManager.CreateRegion(data.CountryCode, data.RegionName);
                if (!createResult.Success)
                {
                    return result.SetFailureResult(
                        $"AddRegion:{createResult.Code}",
                        createResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddRegion:EXCEPTION",
                    $"Failed to add region. Exception: {ex.Message}"
                );
            }
        }

        [HttpDelete("regions/{regionId}")]
        public async Task<FunctionReturnResult> DeleteRegion(string regionId)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteRegion:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var deleteResult = await _regionManager.DeleteRegionSafe(regionId);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteRegion:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteRegion:EXCEPTION",
                    $"Failed to delete region. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("regions/{regionId}/s3")]
        public async Task<FunctionReturnResult> SaveRegionS3(string regionId, [FromBody] RegionS3StorageServerData s3Data)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveRegionS3:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var updateResult = await _regionManager.UpdateRegionS3Config(regionId, s3Data);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveRegionS3:{updateResult.Code}",
                        updateResult.Message
                    );
                }
                
                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveRegionS3:EXCEPTION",
                    $"Failed to save region s3. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("regions/{regionId}/maintenance")]
        public async Task<FunctionReturnResult> ToggleMaintenance(string regionId, [FromForm] bool enabled, [FromForm] string? publicReason, [FromForm] string? privateReason)
        {
            var result = new FunctionReturnResult();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                        Request: Request,
                        checkUserIsAdmin: true,
                        checkUserDisabled: true
                    );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ToggleMaintenance:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var success = false;
                if (enabled)
                {
                    if (string.IsNullOrEmpty(publicReason) || string.IsNullOrEmpty(privateReason))
                    {
                        return result.SetFailureResult(
                            "ToggleMaintenance:INVALID_REASON",
                            "Missing maintenance private or public reason"
                        );
                    }

                    success = await _regionManager.EnableRegionMaintenance(regionId, publicReason, privateReason);
                }
                else
                {
                    success = await _regionManager.DisableRegionMaintenance(regionId);
                }

                if (!success)
                {
                    return result.SetFailureResult(
                        "ToggleMaintenance:FAIL",
                        "Failed to toggle maintenance"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ToggleMaintenance:EXCEPTION",
                    $"Failed to toggle maintenance. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("regions/{regionId}/disabled")]
        public async Task<FunctionReturnResult> ToggleRegionDisabled(string regionId, [FromForm] bool enabled, [FromForm] string? publicReason, [FromForm] string? privateReason)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                        Request: Request,
                        checkUserIsAdmin: true,
                        checkUserDisabled: true
                    );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ToggleRegionDisabled:{validationResult.Code}",
                        validationResult.Message
                    );
                }


                var success = false;
                if (enabled)
                {
                    if (string.IsNullOrEmpty(publicReason) || string.IsNullOrEmpty(privateReason))
                    {
                        return result.SetFailureResult(
                            "ToggleRegionDisabled:INVALID_REASON",
                            "Missing disabled private or public reason"
                        );
                    }

                    success = await _regionManager.DisableRegion(regionId, publicReason, privateReason);
                }
                else
                {
                    success = await _regionManager.EnableRegion(regionId);
                }
                if (!success)
                {
                    return result.SetFailureResult(
                        "ToggleRegionDisabled:FAIL",
                        "Failed to update disabled status"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ToggleRegionDisabled:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        // SERVER MANAGEMENT
        [HttpPost("regions/{regionId}/servers")]
        public async Task<FunctionReturnResult> AddRegionServer(string regionId, [FromBody] CreateUpdateServerRequestModel serverData)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"AddRegionServer:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var addResult = await _regionManager.AddOrUpdateRegionServer("add", regionId, null, serverData);
                if (!addResult.Success)
                {
                    return result.SetFailureResult(
                        "AddRegionServer:FAIL",
                        addResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddRegionServer:EXCEPTION",
                    $"Failed to add server. Exception: {ex.Message}"
                );
            }
        }

        [HttpPut("regions/{regionId}/servers/{serverId}")]
        public async Task<FunctionReturnResult> UpdateRegionServer(string regionId, string serverId, [FromBody] CreateUpdateServerRequestModel serverData)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"UpdateRegionServer:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var updateResult = await _regionManager.AddOrUpdateRegionServer("edit", regionId, serverId, serverData);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        "UpdateRegionServer:FAIL",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdateRegionServer:EXCEPTION",
                    $"Failed to update server. Exception: {ex.Message}"
                );
            }
        }

        [HttpDelete("regions/{regionId}/servers/{serverId}")]
        public async Task<FunctionReturnResult> DeleteRegionServer(string regionId, string serverId)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"UpdateRegionServer:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var deleteResult = await _regionManager.DeleteRegionServerSafe(regionId, serverId);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteServer:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("DeleteServer:EXCEPTION", ex.Message);
            }
        }

        [HttpPost("regions/{regionId}/servers/{serverId}/maintenance")]
        public async Task<FunctionReturnResult> ToggleServerMaintenance(string regionId, string serverId, [FromForm] bool enabled, [FromForm] string? publicReason, [FromForm] string? privateReason)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                        Request: Request,
                        checkUserIsAdmin: true,
                        checkUserDisabled: true
                    );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ToggleServerMaintenance:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var success = false;
                if (enabled)
                {
                    if (string.IsNullOrEmpty(publicReason) || string.IsNullOrEmpty(privateReason))
                    {
                        return result.SetFailureResult(
                            "ToggleServerMaintenance:INVALID_REASON",
                            "Missing maintenance private or public reason"
                        );
                    }

                    success = await _regionManager.EnableRegionServerMaintenance(regionId, serverId, publicReason, privateReason);
                }
                else
                {
                    success = await _regionManager.DisableRegionServerMaintenance(regionId, serverId);
                }
                if (!success)
                {
                    return result.SetFailureResult(
                        "ToggleServerMaintenance:FAIL",
                        "Failed to update server maintenance status"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ToggleServerMaintenance:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("regions/{regionId}/servers/{serverId}/disabled")]
        public async Task<FunctionReturnResult> ToggleServerDisabled(string regionId, string serverId, [FromForm] bool enabled, [FromForm] string? publicReason, [FromForm] string? privateReason)
        {
            var result = new FunctionReturnResult();
            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                        Request: Request,
                        checkUserIsAdmin: true,
                        checkUserDisabled: true
                    );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"ToggleServerDisabled:{validationResult.Code}",
                        validationResult.Message
                    );
                }


                var success = false;
                if (enabled)
                {
                    if (string.IsNullOrEmpty(publicReason) || string.IsNullOrEmpty(privateReason))
                    {
                        return result.SetFailureResult(
                            "ToggleServerDisabled:INVALID_REASON",
                            "Missing disabled private or public reason"
                        );
                    }

                    success = await _regionManager.DisableRegionServer(regionId, serverId, publicReason, privateReason);
                }
                else
                {
                    success = await _regionManager.EnableRegionServer(regionId, serverId);
                }
                if (!success)
                {
                    return result.SetFailureResult(
                        "ToggleServerDisabled:FAIL",
                        "Failed to update server disabled status"
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "ToggleServerDisabled:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}
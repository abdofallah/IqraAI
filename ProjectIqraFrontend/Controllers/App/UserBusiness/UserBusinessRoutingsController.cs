using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using static IqraCore.Interfaces.Validation.IUserBusinessPermissionHelper;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessRoutingsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessRoutingsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/routes/save")]
        public async Task<FunctionReturnResult<BusinessAppRoute?>> SaveBusinessRoute(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "InboundRoutings",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "InboundRoutings",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppRoute? existingRouteData = null;
                if (postType == "edit")  
                {
                    if (!formData.TryGetValue("existingRouteId", out StringValues existingRouteIdStringValue))
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessRoute:MISSING_EXISTING_ROUTE_ID",
                            "Missing existing route id."
                        );
                    }
                    string? existingRouteId = existingRouteIdStringValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingRouteId))
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessRoute:MISSING_EXISTING_ROUTE_ID",
                            "Missing existing route id."
                        );
                    }

                    existingRouteData = await _businessManager.GetRoutesManager().GetBusinessRoute(businessId, existingRouteId);
                    if (existingRouteData == null)
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessRoute:NOT_FOUND",
                            "Existing route not found."
                        );
                    }
                }

                // Process the save/update
                FunctionReturnResult<BusinessAppRoute?> updateResult = await _businessManager.GetRoutesManager().AddOrUpdateUserBusinessRoute(businessId, formData, postType, existingRouteData);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessRoute:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/routes/{routeId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessRoute(long businessId, string routeId)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "InboundRoutings",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "InboundRoutings",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var routeData = await _businessManager.GetRoutesManager().GetBusinessRoute(businessId, routeId);
                if (routeData == null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessRoute:BUSINESS_INBOUND_ROUTINGS_ROUTE_NOT_FOUND",
                        $"Route not found."
                    );
                }

                var deleteResult = await _businessManager.GetRoutesManager().DeleteBusinessRoute(businessId, routeData);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessRoute:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessRoute:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessToolsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessToolsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/tools/save")]
        public async Task<FunctionReturnResult<BusinessAppTool?>> SaveBusinessTool(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppTool?>();

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
                        "SaveBusinessTool:INVALID_POST_TYPE",
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
                            ModulePath = "Tools",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Tools",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTool:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                
                BusinessAppTool? exisitingTool = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("exisitingToolId", out StringValues exisitingToolIdStringValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessTool:MISSING_EXISTING_TOOL_ID",
                            "Missing exisiting tool id."
                        );
                    }
                    string? exisitingToolIdValue = exisitingToolIdStringValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingToolIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessTool:INVALID_EXISTING_TOOL_ID",
                            "Invalid exisiting tool id."
                        );
                    }

                    exisitingTool = await _businessManager.GetToolsManager().GetBusinessAppTool(businessId, exisitingToolIdValue);
                    if (exisitingTool == null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessTool:EXISTING_TOOL_NOT_FOUND",
                            "Existing tool not found."
                        );
                    }
                }

                var updateResult = await _businessManager.GetToolsManager().AddOrUpdateBusinessTool(businessId, formData, postType, exisitingTool);
                if (!updateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTool:{updateResult.Code}",
                        updateResult.Message
                    );
                }

                return result.SetSuccessResult(updateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessTools:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/tools/{toolId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessTool(long businessId, string toolId)
        {
            var result = new FunctionReturnResult();

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
                            ModulePath = "Tools",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Tools",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessTool:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                
                var businessAppToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(businessId, toolId);
                if (businessAppToolData == null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessTool:BUSINESS_APP_TOOL_NOT_FOUND",
                        "Business app tool not found"
                    );
                }

                var deleteResult = await _businessManager.GetToolsManager().DeleteBusinessTool(businessId, businessAppToolData);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessTool:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessTool:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

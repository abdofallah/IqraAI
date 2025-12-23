using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using static IqraCore.Interfaces.Validation.IUserBusinessPermissionHelper;

namespace ProjectIqraFrontend.Controllers.App.UserBusiness
{
    public class UserBusinessScriptsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessScriptsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/scripts/save")]
        public async Task<FunctionReturnResult<BusinessAppScript?>> SaveBusinessScript(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppScript?>();

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
                        "SaveBusinessScript:INVALID_POST_TYPE",
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
                            ModulePath = "Scripts",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Scripts",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessScript:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                
                BusinessAppScript? existingScriptData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("scriptId", out StringValues scriptIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:MISSING_SCRIPT_ID",
                            "Script ID is missing for edit mode"
                        );
                    }
                    string? existingScriptId = scriptIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingScriptId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:INVALID_SCRIPT_ID",
                            "Script ID is required for edit mode"
                        );
                    }
                    existingScriptData = await _businessManager.GetScriptsManager().GetScriptById(businessId, existingScriptId);
                    if (existingScriptData == null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:SCRIPT_NOT_FOUND",
                            "script not found"
                        );
                    }
                }

                var addOrUpdateResult = await _businessManager.GetScriptsManager().AddOrUpdateScript(
                    businessId,
                    postType,
                    formData,
                    existingScriptData
                );
                if (!addOrUpdateResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveBusinessScript:" + addOrUpdateResult.Code,
                        addOrUpdateResult.Message
                    );
                }

                return result.SetSuccessResult(addOrUpdateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessScript:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/scripts/{scriptId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessScript(long businessId, string scriptId)
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
                            ModulePath = "Scripts",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Scripts",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessScript:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                
                var scriptData = await _businessManager.GetScriptsManager().GetScriptById(businessId, scriptId);
                if (scriptData == null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessScript:SCRIPT_NOT_FOUND",
                        "script not found"
                    );
                }

                var deleteResult = await _businessManager.GetScriptsManager().DeleteScript(businessId, scriptData);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessScript:" + deleteResult.Code,
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessScript:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }
    }
}

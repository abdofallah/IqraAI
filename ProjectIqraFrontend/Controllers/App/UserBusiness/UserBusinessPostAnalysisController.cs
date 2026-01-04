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
    public class UserBusinessPostAnalysisController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessPostAnalysisController(
            ISessionValidationAndPermissionHelper userSessionValidationHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        )
        {
            _userSessionValidationAndPermissionHelper = userSessionValidationHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/postanalysis/save")]
        public async Task<FunctionReturnResult<BusinessAppPostAnalysis?>> SavePostAnalysisTemplate(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppPostAnalysis?>();

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
                        "SavePostAnalysisTemplate:INVALID_POST_TYPE",
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
                            ModulePath = "PostAnalysis",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "PostAnalysis",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePostAnalysisTemplate:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppPostAnalysis? existingTemplateData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("existingTemplateId", out StringValues existingIdValue))
                    {
                        return result.SetFailureResult(
                            "SavePostAnalysisTemplate:MISSING_TEMPLATE_ID",
                            "Existing Template ID is required for edit mode."
                        );
                    }
                    string? existingTemplateId = existingIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingTemplateId))
                    {
                        return result.SetFailureResult(
                            "SavePostAnalysisTemplate:MISSING_TEMPLATE_ID",
                            "Existing Template ID is required for edit mode."
                        );
                    }

                    var getTemplateResult = await _businessManager.GetPostAnalysisManager().GetTemplateById(businessId, existingTemplateId);
                    if (!getTemplateResult.Success)
                    {
                        return result.SetFailureResult(
                            $"SavePostAnalysisTemplate:{getTemplateResult.Code}",
                            getTemplateResult.Message
                        );
                    }
                    existingTemplateData = getTemplateResult.Data;
                }

                var addOrUpdateResult = await _businessManager.GetPostAnalysisManager().AddOrUpdateTemplateAsync(businessId, formData, postType, existingTemplateData);
                if (!addOrUpdateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SavePostAnalysisTemplate:{addOrUpdateResult.Code}",
                        addOrUpdateResult.Message
                    );
                }

                return result.SetSuccessResult(addOrUpdateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SavePostAnalysisTemplate:EXCEPTION",
                    $"Error saving post analysis template: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/postanalysis/{templateId}/delete")]
        public async Task<FunctionReturnResult> DeletePostAnalysisTemplate(long businessId, string templateId)
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
                            ModulePath = "PostAnalysis",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "PostAnalysis",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeletePostAnalysisTemplate:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var postAnalysisData = await _businessManager.GetPostAnalysisManager().GetTemplateById(businessId, templateId);
                if (!postAnalysisData.Success || postAnalysisData.Data == null)
                {
                    return result.SetFailureResult(
                        "DeletePostAnalysisTemplate:TEMPLATE_NOT_FOUND",
                        "Template not found."
                    );
                }

                var deleteResult = await _businessManager.GetPostAnalysisManager().DeleteTemplate(businessId, postAnalysisData.Data);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeletePostAnalysisTemplate:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeletePostAnalysisTemplate:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

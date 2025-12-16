using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.UserBusiness
{
    public class UserBusinessScriptsController : Controller
    {
        private readonly BusinessManager _businessManager;
        private readonly UserSessionValidationHelper _userSessionValidationHelper;

        public UserBusinessScriptsController(
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            UserSessionValidationHelper userSessionValidationHelper
        )
        {
            _businessManager = businessManager;
            _userSessionValidationHelper = userSessionValidationHelper;
        }

        [HttpPost("/app/user/business/{businessId}/scripts/save")]
        public async Task<FunctionReturnResult<BusinessAppScript?>> SaveBusinessScript(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppScript?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                    Request,
                    businessId,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessScript:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Scripts Permission
                if (businessData.Permission.Scripts.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessScript:BUSINESS_SCRIPTS_DISABLED_FULL",
                        $"Business does not have permission to access agents{(string.IsNullOrEmpty(businessData.Permission.Scripts.DisabledFullReason) ? "." : ": " + businessData.Permission.Scripts.DisabledFullReason)}"
                    );
                }
                if (businessData.Permission.Scripts.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessScript:BUSINESS_SCRIPTS_DISABLED_EDITING",
                        $"Business does not have permission to edit agents{(string.IsNullOrEmpty(businessData.Permission.Scripts.DisabledEditingReason) ? "." : ": " + businessData.Permission.Scripts.DisabledEditingReason)}"
                    );
                }

                // Post type validation
                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveBusinessScript:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Script validation for edit mode
                BusinessAppScript? existingScriptData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("scriptId", out StringValues scriptIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:MISSING_AGENT_SCRIPT_ID",
                            "Agent Script ID is missing for edit mode"
                        );
                    }
                    string? existingScriptId = scriptIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingScriptId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:INVALID_AGENT_SCRIPT_ID",
                            "Agent Script ID is required for edit mode"
                        );
                    }
                    existingScriptData = await _businessManager.GetScriptsManager().GetScriptById(businessId, existingScriptId);
                    if (existingScriptData == null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessScript:AGENT_SCRIPT_NOT_FOUND",
                            "Agent script not found"
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
    }
}

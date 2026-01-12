using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Interfaces.Validation;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminIntegrationsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminIntegrationsController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            IntegrationsManager integrationsManager
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
            _integrationsManager = integrationsManager;
        }

        [HttpGet("/app/admin/integrations")]
        public async Task<FunctionReturnResult<List<IntegrationData>?>> GetIntegrations(int page = 0, int pageSize = 100)
        {
            var result = new FunctionReturnResult<List<IntegrationData>?>();

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
                        $"GetIntegrations:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var integrationsResult = await _integrationsManager.GetIntegrationsList(page, pageSize);
                if (!integrationsResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetIntegrations:{integrationsResult.Code}",
                        integrationsResult.Message
                    );
                }

                return result.SetSuccessResult(integrationsResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetIntegrations:EXCEPTION",
                    $"Failed to get integrations. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/integrations/save")]
        public async Task<FunctionReturnResult<IntegrationData?>> SaveIntegration([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<IntegrationData?>();

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
                        $"SaveIntegration:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    return result.SetFailureResult(
                        "SaveIntegration:CHANGES_NOT_FOUND",
                        "Changes data not found"
                    );
                }

                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
                {
                    return result.SetFailureResult(
                        "SaveIntegration:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                formData.TryGetValue("currentIntegrationId", out StringValues currentIntegrationIdValue);
                string? currentIntegrationId = currentIntegrationIdValue.ToString();

                if (postType == "edit")
                {
                    if (string.IsNullOrWhiteSpace(currentIntegrationId))
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:MISSING_EXISTING_INTEGRATION_ID",
                            "Missing existing integration id"
                        );
                    }

                    bool integrationExists = await _integrationsManager.IntegrationExists(currentIntegrationId);
                    if (!integrationExists)
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:EXISTING_INTEGRATION_NOT_FOUND",
                            "Existing integration not found"
                        );
                    }
                }
                else if (postType == "new")
                {
                    bool integrationExists = await _integrationsManager.IntegrationExists(currentIntegrationId);
                    if (integrationExists)
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:INTEGRATION_ALREADY_EXISTS",
                            "Integration already exists with id"
                        );
                    }
                }

                // Handle logo file if present
                IFormFile? integrationLogo = formData.Files.FirstOrDefault(x => x.Name == "logo");
                if (integrationLogo == null && postType == "new")
                {
                    return result.SetFailureResult(
                        "SaveIntegration:LOGO_NOT_FOUND",
                        "Logo file not found"
                    );
                }
                else if (integrationLogo != null)
                {
                    int logoValidateResult = ImageHelper.ValidateBusinessLogoFile(integrationLogo);

                    if (logoValidateResult == 0)
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:LOGO_TOO_BIG",
                            "The integration logo file is too big. Maximum size is 5MB."
                        );
                    }
                    else if (logoValidateResult == 1)
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:LOGO_NOT_VALID",
                            "The integration logo file is not valid."
                        );
                    }
                    else if (logoValidateResult != 200)
                    {
                        return result.SetFailureResult(
                            "SaveIntegration:LOGO_NOT_VALID",
                            $"The integration logo file is not valid. Error code: {logoValidateResult}"
                        );
                    }
                }

                var saveResult = await _integrationsManager.AddOrUpdateIntegration(
                    changesJsonString.ToString(),
                    postType,
                    currentIntegrationId,
                    integrationLogo
                );

                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveIntegration:{saveResult.Code}",
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveIntegration:EXCEPTION",
                    $"Failed to save integration. Exception: {ex.Message}"
                );
            }
        }
    }
}

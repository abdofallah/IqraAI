using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessIntegrationsController : Controller
    {
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly UserSessionValidationHelper _userSessionValidationHelper;

        public UserBusinessIntegrationsController(
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            UserSessionValidationHelper userSessionValidationHelper
        ) {
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
            _userSessionValidationHelper = userSessionValidationHelper;
        }

        [HttpPost("/app/user/business/{businessId}/integrations/save")]
        public async Task<FunctionReturnResult<BusinessAppIntegration?>> SaveBusinessIntegration(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppIntegration?>();

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
                        $"SaveBusinessIntegration:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Integration permission validation
                if (businessData.Permission.Integrations.DisabledFullAt != null)
                {
                    var message = "Business does not have permission to access integrations";
                    if (!string.IsNullOrEmpty(businessData.Permission.Integrations.DisabledFullReason))
                    {
                        message += ": " + businessData.Permission.Integrations.DisabledFullReason;
                    }

                    return result.SetFailureResult(
                        "SaveBusinessIntegration:BUSINESS_INTEGRATIONS_DISABLED_FULL",
                        message
                    );
                }

                // Post type validation
                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveBusinessIntegration:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                // Check operation-specific permissions and validate existing integration
                formData.TryGetValue("currentIntegrationId", out StringValues currentIntegrationIdValue);
                string? currentIntegrationId = currentIntegrationIdValue.ToString();

                formData.TryGetValue("currentIntegrationType", out StringValues currentIntegrationTypeValue);
                string? currentIntegrationType = currentIntegrationTypeValue.ToString();
                if (string.IsNullOrWhiteSpace(currentIntegrationType))
                {
                    return result.SetFailureResult(
                        "SaveBusinessIntegration:INVALID_INTEGRATION_TYPE",
                        "Invalid integration type"
                    );
                }

                var currentIntergationTypeData = await _integrationsManager.getIntegrationData(currentIntegrationType);
                if (!currentIntergationTypeData.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessIntegration:{currentIntergationTypeData.Code}",
                        currentIntergationTypeData.Message
                    );
                }

                if (currentIntergationTypeData.Data!.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessIntegration:INTEGRATION_TYPE_DISABLED",
                        "Current integration type is currently disabled"
                    );
                }

                BusinessAppIntegration? businessIntegrationData = null;
                if (postType == "edit")
                {
                    if (businessData.Permission.Integrations.DisabledEditingAt != null)
                    {
                        var message = "Business does not have permission to edit integrations";
                        if (!string.IsNullOrEmpty(businessData.Permission.Integrations.DisabledEditingReason))
                        {
                            message += ": " + businessData.Permission.Integrations.DisabledEditingReason;
                        }

                        return result.SetFailureResult(
                            "SaveBusinessIntegration:BUSINESS_INTEGRATIONS_DISABLED_EDITING",
                            message
                        );
                    }

                    if (string.IsNullOrWhiteSpace(currentIntegrationId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessIntegration:MISSING_EXISTING_INTEGRATION_ID",
                            "Missing existing integration id"
                        );
                    }

                    var businessIntegrationResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, currentIntegrationId);
                    if (!businessIntegrationResult.Success)
                    {
                        return result.SetFailureResult(
                            $"SaveBusinessIntegration:{businessIntegrationResult.Code}",
                            businessIntegrationResult.Message
                        );
                    }

                    businessIntegrationData = businessIntegrationResult.Data;
                }
                else if (postType == "new")
                {
                    if (businessData.Permission.Integrations.DisabledAddingAt != null)
                    {
                        var message = "Business does not have permission to add integrations";
                        if (!string.IsNullOrEmpty(businessData.Permission.Integrations.DisabledAddingReason))
                        {
                            message += ": " + businessData.Permission.Integrations.DisabledAddingReason;
                        }

                        return result.SetFailureResult(
                            "SaveBusinessIntegration:BUSINESS_INTEGRATIONS_DISABLED_ADDING",
                            message
                        );
                    }
                }

                // Process the integration
                var saveResult = await _businessManager.GetIntegrationsManager().AddOrUpdateBusinessIntegration(
                    businessId,
                    formData,
                    postType,
                    currentIntergationTypeData.Data,
                    businessIntegrationData,
                    _integrationsManager
                );

                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessIntegration:{saveResult.Code}",
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessIntegration:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/integrations/{integrationId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessIntegration(long businessId, string integrationId)
        {
            var result = new FunctionReturnResult();

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
                        $"DeleteBusinessIntegration:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Integration permission validation
                if (businessData.Permission.Integrations.DisabledFullAt != null)
                {
                    var message = "Business does not have permission to access integrations";
                    if (!string.IsNullOrEmpty(businessData.Permission.Integrations.DisabledFullReason))
                    {
                        message += ": " + businessData.Permission.Integrations.DisabledFullReason;
                    }

                    return result.SetFailureResult(
                        "DeleteBusinessIntegration:BUSINESS_INTEGRATIONS_DISABLED_FULL",
                        message
                    );
                }

                if (businessData.Permission.Integrations.DisabledDeletingAt != null)
                {
                    var message = "Business does not have permission to delete integrations";
                    if (!string.IsNullOrEmpty(businessData.Permission.Integrations.DisabledDeletingReason))
                    {
                        message += ": " + businessData.Permission.Integrations.DisabledDeletingReason;
                    }

                    return result.SetFailureResult(
                        "DeleteBusinessIntegration:BUSINESS_INTEGRATIONS_DISABLED_DELETING",
                        message
                    );
                }

                var integrationData = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, integrationId);
                if (!integrationData.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessIntegration:{integrationData.Code}",
                        integrationData.Message
                    );
                }

                var deleteResult = await _businessManager.GetIntegrationsManager().DeleteBusinessIntegration(businessId, integrationData.Data!);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessIntegration:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "DeleteBusinessIntegration:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

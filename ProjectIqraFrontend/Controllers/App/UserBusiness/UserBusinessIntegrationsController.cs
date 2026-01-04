using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessIntegrationsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;

        public UserBusinessIntegrationsController(
            ISessionValidationAndPermissionHelper userSessionValidationHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/user/business/{businessId}/integrations/save")]
        public async Task<FunctionReturnResult<BusinessAppIntegration?>> SaveBusinessIntegration(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppIntegration?>();

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
                        "SaveBusinessIntegration:INVALID_POST_TYPE",
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
                            ModulePath = "Integrations",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Integrations",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessIntegration:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                if (!formData.TryGetValue("currentIntegrationType", out StringValues currentIntegrationTypeValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessIntegration:MISSING_INTEGRATION_TYPE",
                        "Missing integration type"
                    );
                }
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
                    if (!formData.TryGetValue("currentIntegrationId", out StringValues currentIntegrationIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessIntegration:MISSING_EXISTING_INTEGRATION_ID",
                            "Missing existing integration id"
                        );
                    }
                    string? currentIntegrationId = currentIntegrationIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(currentIntegrationId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessIntegration:EMPTY_EXISTING_INTEGRATION_ID",
                            "Empty existing integration id"
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
                            ModulePath = "Integrations",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Integrations",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessIntegration:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
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

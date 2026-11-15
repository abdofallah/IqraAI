using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Validation;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.Business.App.Integration;
using IqraInfrastructure.Managers.Integrations.OAuth;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessIntegrationOAuthController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly IntegrationOAuthManager _integrationOAuthManager;

        private const string OAuthResultViewPath = "~/Views/AppView/Integrations/OAuthResult.cshtml";

        public UserBusinessIntegrationOAuthController(
            ISessionValidationAndPermissionHelper userSessionValidationHelper,
            WhiteLabelContext? whiteLabelContext,
            IntegrationOAuthManager integrationOAuthManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationHelper;
            _whiteLabelContext = whiteLabelContext;
            _integrationOAuthManager = integrationOAuthManager;
        }

        [HttpGet("/app/user/business/{businessId}/integrations/oauth/status")]
        public async Task<FunctionReturnResult<List<BusinessAppIntegrationOAuthStatusModel>?>> GetBusinessIntegrationsOAuthStatus(long businessId)
        {
            var result = new FunctionReturnResult<List<BusinessAppIntegrationOAuthStatusModel>?>();

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
                            Type = BusinessModulePermissionType.Retrieving,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessIntegrationsOAuthStatus:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var statusResult = await _integrationOAuthManager.GetConnectionStatuses(businessId);
                if (!statusResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessIntegrationsOAuthStatus:{statusResult.Code}",
                        statusResult.Message
                    );
                }

                return result.SetSuccessResult(statusResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessIntegrationsOAuthStatus:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/integrations/oauth/redirect-uri")]
        public async Task<FunctionReturnResult<string?>> GetBusinessIntegrationOAuthRedirectUri(long businessId)
        {
            var result = new FunctionReturnResult<string?>();

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
                            Type = BusinessModulePermissionType.Retrieving,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessIntegrationOAuthRedirectUri:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                return result.SetSuccessResult(_integrationOAuthManager.BuildRedirectUri($"{Request.Scheme}://{Request.Host}"));
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessIntegrationOAuthRedirectUri:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/integrations/{integrationId}/oauth/connect")]
        public async Task<IActionResult> ConnectBusinessIntegrationOAuth(long businessId, string integrationId)
        {
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
                            Type = BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, $"ConnectBusinessIntegrationOAuth:{userSessionAndBusinessValidationResult.Code}"));
                }

                var connectResult = await _integrationOAuthManager.BeginConnect(businessId, integrationId, $"{Request.Scheme}://{Request.Host}");
                if (!connectResult.Success)
                {
                    return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, $"ConnectBusinessIntegrationOAuth:{connectResult.Code}"));
                }

                return Redirect(connectResult.Data!);
            }
            catch (Exception)
            {
                return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, "ConnectBusinessIntegrationOAuth:EXCEPTION"));
            }
        }

        [HttpPost("/app/user/business/{businessId}/integrations/{integrationId}/oauth/test")]
        public async Task<FunctionReturnResult<BusinessAppIntegrationOAuthStatusModel?>> TestBusinessIntegrationOAuthConnection(long businessId, string integrationId)
        {
            var result = new FunctionReturnResult<BusinessAppIntegrationOAuthStatusModel?>();

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
                            Type = BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"TestBusinessIntegrationOAuthConnection:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var testResult = await _integrationOAuthManager.TestConnection(businessId, integrationId);
                if (!testResult.Success)
                {
                    return result.SetFailureResult(
                        $"TestBusinessIntegrationOAuthConnection:{testResult.Code}",
                        testResult.Message,
                        testResult.Data
                    );
                }

                return result.SetSuccessResult(testResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "TestBusinessIntegrationOAuthConnection:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/integrations/oauth/callback")]
        public async Task<IActionResult> BusinessIntegrationOAuthCallback(string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription)
        {
            long? businessId = null;

            try
            {
                var stateResult = _integrationOAuthManager.ReadState(state);
                if (!stateResult.Success)
                {
                    return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(null, $"BusinessIntegrationOAuthCallback:{stateResult.Code}"));
                }
                businessId = stateResult.Data!.BusinessId;

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: stateResult.Data.BusinessId,
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
                            Type = BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, $"BusinessIntegrationOAuthCallback:{userSessionAndBusinessValidationResult.Code}"));
                }

                var completeResult = await _integrationOAuthManager.CompleteConnect(stateResult.Data, code, error, errorDescription, $"{Request.Scheme}://{Request.Host}");
                if (!completeResult.Success)
                {
                    return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, $"BusinessIntegrationOAuthCallback:{completeResult.Code}"));
                }

                return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, null));
            }
            catch (Exception)
            {
                return View(OAuthResultViewPath, _integrationOAuthManager.BuildConnectResult(businessId, "BusinessIntegrationOAuthCallback:EXCEPTION"));
            }
        }
    }
}

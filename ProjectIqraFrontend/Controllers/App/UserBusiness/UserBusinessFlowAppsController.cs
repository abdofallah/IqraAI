using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.FlowApp;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.FlowApp;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.FlowApp;
using Microsoft.AspNetCore.Mvc;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.UserBusiness
{
    [Route("app/user/business/{businessId}/flowapps")]
    public class UserBusinessFlowAppsController : ControllerBase
    {
        private readonly FlowAppManager _flowAppManager;
        private readonly BusinessManager _businessManager;
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;

        public UserBusinessFlowAppsController(
            FlowAppManager flowAppManager,
            BusinessManager businessManager,
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext
        ) {
            _flowAppManager = flowAppManager;
            _businessManager = businessManager;
            _whiteLabelContext = whiteLabelContext;
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
        }

        [HttpPost("{appKey}/fetchers/{fetcherKey}")]
        public async Task<FunctionReturnResult<List<DynamicOption>?>> FetchOptions(
            long businessId,
            string appKey,
            string fetcherKey,
            [FromBody] FlowAppFetchOptionsRequestModel request
        ) {
            var result = new FunctionReturnResult<List<DynamicOption>?>();

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
                            ModulePath = "FlowApps.FlowAppsPermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "FlowApps.Fetchers",
                            Type = BusinessModulePermissionType.Executing,
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

                var appDef = await _flowAppManager.GetAppDefinitionWithPermissionsAsync(appKey);
                if (appDef == null)
                {
                    return result.SetFailureResult("FetchOptions:APP_NOT_FOUND", "App not found.");
                }

                var fetcherDef = appDef.Fetchers.FirstOrDefault(f => f.FetcherKey == fetcherKey);
                if (fetcherDef == null)
                {
                    return result.SetFailureResult("FetchOptions:FETCHER_NOT_FOUND", "Fetcher not found.");
                }

                BusinessAppIntegration? integration = null;
                if (fetcherDef.RequiresIntegration)
                {
                    if (string.IsNullOrEmpty(request.IntegrationId))
                    {
                        return result.SetFailureResult("FetchOptions:INTEGRATION_ID_REQUIRED", "Integration id is required.");
                    }

                    var integResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, request.IntegrationId);
                    if (!integResult.Success)
                    {
                        return result.SetFailureResult("FetchOptions:INTEGRATION_NOT_FOUND", "Business integration not found.");
                    }
                    integration = integResult.Data!;
                }
                
                var fetchOptionsResult = await _flowAppManager.FetchOptionsAsync(
                    appKey,
                    fetcherKey,
                    request.Context,
                    integration
                );
                if (!fetchOptionsResult.Success)
                {
                    return result.SetFailureResult(
                        $"FetchOptions:{fetchOptionsResult.Code}",
                        fetchOptionsResult.Message
                    );
                }

                return result.SetSuccessResult(fetchOptionsResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "FetchOptions:EXCEPTION",
                    $"Failed to fetch options. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("{appKey}/actions/{actionKey}/test")]
        public async Task<FunctionReturnResult<ActionExecutionResult?>> TestAction(
            long businessId,
            string appKey,
            string actionKey,
            [FromBody] FlowAppTestActionRequestModel request
        ) {
            var result = new FunctionReturnResult<ActionExecutionResult?>();

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
                            ModulePath = "FlowApps.FlowAppsPermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "FlowApps.FlowAppsPermissions",
                            Type = BusinessModulePermissionType.Executing,
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

                var appDef = await _flowAppManager.GetAppDefinitionWithPermissionsAsync(appKey);
                if (appDef == null)
                {
                    return result.SetFailureResult("TestAction:APP_NOT_FOUND", "App not found.");
                }

                var actionDef = appDef.Actions.FirstOrDefault(f => f.ActionKey == actionKey);
                if (actionDef == null)
                {
                    return result.SetFailureResult("TestAction:ACTION_NOT_FOUND", "Action not found.");
                }

                BusinessAppIntegration? integration = null;
                if (actionDef.RequiresIntegration)
                {
                    if (string.IsNullOrEmpty(request.IntegrationId))
                    {
                        return result.SetFailureResult("TestAction:INTEGRATION_ID_REQUIRED", "Integration id is required.");
                    }

                    var integResult = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(businessId, request.IntegrationId);
                    if (!integResult.Success)
                    {
                        return result.SetFailureResult("TestAction:INTEGRATION_NOT_FOUND", "Business integration not found.");
                    }
                    integration = integResult.Data!;
                }

                var executeResult = await _flowAppManager.ExecuteActionAsync(
                    appKey,
                    actionKey,
                    request.Inputs,
                    new Dictionary<string, object?>(), // Empty session context
                    integration
                );
                if (!executeResult.Success)
                {
                    return result.SetFailureResult(
                        $"TestAction:{executeResult.Code}",
                        executeResult.Message
                    );
                }

                return result.SetSuccessResult(executeResult);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "TestAction:EXCEPTION",
                    $"Failed to fetch options. Exception: {ex.Message}"
                );
            }
        }
    }
}
using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Services;
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
        private readonly ToolTestRateLimiter _rateLimiter;
        private readonly EndpointSecurityService _endpointSecurityService;
        private readonly BusinessToolTestExecutionRepository _testExecutionRepository;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<UserBusinessToolsController> _logger;

        public UserBusinessToolsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager,
            ToolTestRateLimiter rateLimiter,
            EndpointSecurityService endpointSecurityService,
            BusinessToolTestExecutionRepository testExecutionRepository,
            ILoggerFactory loggerFactory
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
            _rateLimiter = rateLimiter;
            _endpointSecurityService = endpointSecurityService;
            _testExecutionRepository = testExecutionRepository;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<UserBusinessToolsController>();
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

        [HttpPost("/app/user/business/{businessId}/tools/{toolId}/test")]
        public async Task<FunctionReturnResult<BusinessToolTestResult?>> TestBusinessTool(long businessId, string toolId, [FromBody] BusinessToolTestRequest testRequest)
        {
            var result = new FunctionReturnResult<BusinessToolTestResult?>();

            try
            {
                // Rate Limit Check
                string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: false,
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: false,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Tools",
                            Type = BusinessModulePermissionType.Full,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"TestBusinessTool:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                string userEmail = userSessionAndBusinessValidationResult.Data?.userData?.Email ?? "unknown";

                // Rate limit after auth so we can check both IP and email
                var (isAllowed, rateLimitReason) = _rateLimiter.CheckRateLimit(clientIp, userEmail);
                if (!isAllowed)
                {
                    Response.StatusCode = 429;
                    return result.SetFailureResult(
                        "TestBusinessTool:RATE_LIMIT",
                        rateLimitReason ?? "Rate limit exceeded. Please wait before running another test."
                    );
                }

                // Get Business App (contains Tools list)
                var businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, userEmail);
                if (!businessAppResult.Success || businessAppResult.Data == null)
                {
                    return result.SetFailureResult(
                        "TestBusinessTool:BUSINESS_APP_NOT_FOUND",
                        "Business app data not found."
                    );
                }
                var businessApp = businessAppResult.Data;

                // Find Tool
                var toolData = businessApp.Tools?.Find(t => t.Id == toolId);
                if (toolData == null)
                {
                    return result.SetFailureResult(
                        "TestBusinessTool:TOOL_NOT_FOUND",
                        $"Tool with id '{toolId}' not found in business."
                    );
                }

                // Execute Test
                var testResult = await _businessManager.GetToolsManager().TestBusinessTool(
                    businessId,
                    toolData,
                    businessApp,
                    testRequest.Parameters,
                    testRequest.LanguageCode,
                    _loggerFactory,
                    _endpointSecurityService
                );

                // Persist test execution (fire-and-forget, don't block response)
                if (testResult.Success && testResult.Data != null)
                {
                    string toolName = toolData.General.Name.TryGetValue(testRequest.LanguageCode, out string? name)
                        ? name ?? toolData.Id
                        : toolData.Id;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _testExecutionRepository.CreateAsync(new BusinessToolTestExecution
                            {
                                BusinessId = businessId,
                                ToolId = toolId,
                                ToolName = toolName,
                                ExecutedByEmail = userEmail,
                                ExecutedByIp = clientIp,
                                ExecutedAt = DateTime.UtcNow,
                                InputParameters = testRequest.Parameters,
                                LanguageCode = testRequest.LanguageCode,
                                Result = testResult.Data
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to persist tool test execution for tool {ToolId} in business {BusinessId}.", toolId, businessId);
                        }
                    });
                }

                return testResult;
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "TestBusinessTool:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

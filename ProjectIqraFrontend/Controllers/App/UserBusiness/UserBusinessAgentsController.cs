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
    public class UserBusinessAgentsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessAgentsController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager
        ) {
            _businessManager = businessManager;
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
        }

        [HttpPost("/app/user/business/{businessId}/agents/save")]
        public async Task<FunctionReturnResult<BusinessAppAgent?>> SaveBusinessAgent(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppAgent?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                ) {
                    return result.SetFailureResult(
                        "SaveBusinessAgent:INVALID_POST_TYPE",
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
                            ModulePath = "Agents",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Agents",
                            Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessAgent:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                BusinessAppAgent? exisitingAgentData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("agentId", out StringValues existingAgentIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:MISSING_EXISTING_AGENT_ID",
                            "Existing Agent ID is required for edit mode."
                        );
                    }
                    string? exisitingAgentId = existingAgentIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingAgentId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:INVALID_EXISTING_AGENT_ID",
                            "Existing Agent ID is invalid."
                        );
                    }

                    exisitingAgentData = await _businessManager.GetAgentsManager().GetAgentById(businessId, exisitingAgentId);
                    if (exisitingAgentData == null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:AGENT_DOES_NOT_EXIST",
                            "Agent does not exist for business."
                        );
                    }
                }

                // Forward Result
                var addOrUpdateResult = await _businessManager.GetAgentsManager().AddOrUpdateAgent(businessId, postType, formData, exisitingAgentData);
                if (!addOrUpdateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveBusinessAgent:{addOrUpdateResult.Code}",
                        addOrUpdateResult.Message
                    );
                }

                return result.SetSuccessResult(addOrUpdateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessAgent:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
            
        }

        [HttpPost("/app/user/business/{businessId}/agents/{agentId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessAgent(long businessId, string agentId)
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
                            ModulePath = "Agents",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Agents",
                            Type = BusinessModulePermissionType.Deleting,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessAgent:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var agentData = await _businessManager.GetAgentsManager().GetAgentById(businessId, agentId);
                if (agentData == null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessAgent:AGENT_DOES_NOT_EXIST",
                        "Agent does not exist for business."
                    );
                }

                var deleteResult = await _businessManager.GetAgentsManager().DeleteAgent(businessId, agentData);
                if (!deleteResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessAgent:{deleteResult.Code}",
                        deleteResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessAgent:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }
    }
}

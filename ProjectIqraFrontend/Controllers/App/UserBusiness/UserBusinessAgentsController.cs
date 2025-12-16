using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessAgentsController : Controller
    {
        private readonly BusinessManager _businessManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly UserSessionValidationHelper _userSessionValidationHelper;

        public UserBusinessAgentsController(
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager,
            UserSessionValidationHelper userSessionValidationHelper
        )
        {
            _businessManager = businessManager;
            _llmProviderManager = llmProviderManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
            _userSessionValidationHelper = userSessionValidationHelper;
        }

        [HttpPost("/app/user/business/{businessId}/agents/save")]
        public async Task<FunctionReturnResult<BusinessAppAgent?>> SaveBusinessAgent(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppAgent?>();

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
                        $"SaveBusinessAgent:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Business Agents Permission
                if (businessData.Permission.Agents.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgent:BUSINESS_AGENTS_DISABLED_FULL",
                        $"Business does not have permission to access agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledFullReason) ? "." : ": " + businessData.Permission.Agents.DisabledFullReason)}"
                    );
                }

                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgent:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                string? exisitingAgentId = null;
                if (postType == "new")
                {
                    if (businessData.Permission.Agents.DisabledAddingAt != null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:BUSINESS_AGENTS_DISABLED_ADDING",
                            $"Business does not have permission to add new agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledAddingReason) ? "." : ": " + businessData.Permission.Agents.DisabledAddingReason)}"
                        );
                    }
                }
                else if (postType == "edit")
                {
                    if (businessData.Permission.Agents.DisabledEditingAt != null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:BUSINESS_AGENTS_DISABLED_EDITING",
                            $"Business does not have permission to edit agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledEditingReason) ? "." : ": " + businessData.Permission.Agents.DisabledEditingReason)}"
                        );
                    }

                    if (!formData.TryGetValue("agentId", out StringValues existingAgentIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:MISSING_EXISTING_AGENT_ID",
                            "Existing Agent ID is required for edit mode."
                        );
                    }
                    exisitingAgentId = existingAgentIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(exisitingAgentId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:INVALID_EXISTING_AGENT_ID",
                            "Existing Agent ID is invalid."
                        );
                    }

                    var checkAgentExists = await _businessManager.GetAgentsManager().CheckAgentExists(businessId, exisitingAgentId);
                    if (checkAgentExists == false)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgent:AGENT_DOES_NOT_EXIST",
                            "Agent does not exist for business."
                        );
                    }
                }

                // Forward Result
                var addOrUpdateResult = await _businessManager.GetAgentsManager().AddOrUpdateAgent(businessId, postType, formData, exisitingAgentId, _llmProviderManager, _sttProviderManager, _ttsProviderManager);
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

        [HttpPost("/app/user/business/{businessId}/agents/script/save")]
        public async Task<FunctionReturnResult<BusinessAppAgentScript?>> SaveBusinessAgentScript(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppAgentScript?>();

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
                        $"SaveBusinessAgentScript:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Agents Permission
                if (businessData.Permission.Agents.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:BUSINESS_AGENTS_DISABLED_FULL",
                        $"Business does not have permission to access agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledFullReason) ? "." : ": " + businessData.Permission.Agents.DisabledFullReason)}"
                    );
                }
                if (businessData.Permission.Agents.DisabledEditingAt != null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:BUSINESS_AGENTS_DISABLED_EDITING",
                        $"Business does not have permission to edit agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledEditingReason) ? "." : ": " + businessData.Permission.Agents.DisabledEditingReason)}"
                    );
                }

                // Post type validation
                string? postType = formData["postType"].ToString();
                if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Agent Id/Data validation
                if (!formData.TryGetValue("agentId", out StringValues agentIdValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:MISSING_AGENT_ID",
                        "Agent ID is missing."
                    );
                }
                var agentId = agentIdValue.ToString();
                if (string.IsNullOrWhiteSpace(agentId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:INVALID_AGENT_ID",
                        "Agent ID is required."
                    );
                }
                var agentExists = await _businessManager.GetAgentsManager().CheckAgentExists(businessId, agentId);
                if (!agentExists)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:AGENT_NOT_FOUND",
                        "Agent not found for business."
                    );
                }

                // Script validation for edit mode
                BusinessAppAgentScript? existingScriptData = null;
                if (postType == "edit")
                {
                    if (!formData.TryGetValue("scriptId", out StringValues scriptIdValue))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgentScript:MISSING_AGENT_SCRIPT_ID",
                            "Agent Script ID is missing for edit mode"
                        );
                    }
                    string? existingScriptId = scriptIdValue.ToString();
                    if (string.IsNullOrWhiteSpace(existingScriptId))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgentScript:INVALID_AGENT_SCRIPT_ID",
                            "Agent Script ID is required for edit mode"
                        );
                    }
                    existingScriptData = await _businessManager.GetAgentsManager().GetAgentScriptById(businessId, agentId, existingScriptId);
                    if (existingScriptData == null)
                    {
                        return result.SetFailureResult(
                            "SaveBusinessAgentScript:AGENT_SCRIPT_NOT_FOUND",
                            "Agent script not found"
                        );
                    }
                }

                var addOrUpdateResult = await _businessManager.GetAgentsManager().AddOrUpdateAgentScript(
                    businessId,
                    agentId,
                    postType,
                    formData,
                    existingScriptData
                );
                if (!addOrUpdateResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveBusinessAgentScript:" + addOrUpdateResult.Code,
                        addOrUpdateResult.Message
                    );
                }

                return result.SetSuccessResult(addOrUpdateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessAgentScript:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }
    }

}

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

                BusinessAppAgent? exisitingAgentData = null;
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
                var addOrUpdateResult = await _businessManager.GetAgentsManager().AddOrUpdateAgent(businessId, postType, formData, exisitingAgentData, _llmProviderManager, _sttProviderManager, _ttsProviderManager);
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
                        $"DeleteBusinessAgent:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;
                var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

                // Business Agents Permission
                if (businessData.Permission.Agents.DisabledFullAt != null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessAgent:BUSINESS_AGENTS_DISABLED_FULL",
                        $"Business does not have permission to access agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledFullReason) ? "." : ": " + businessData.Permission.Agents.DisabledFullReason)}"
                    );
                }

                if (businessData.Permission.Agents.DisabledDeletingAt != null)
                {
                    return result.SetFailureResult(
                        "DeleteBusinessAgent:BUSINESS_AGENTS_DISABLED_FULL",
                        $"Business does not have permission to access agents{(string.IsNullOrEmpty(businessData.Permission.Agents.DisabledDeletingReason) ? "." : ": " + businessData.Permission.Agents.DisabledDeletingReason)}"
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

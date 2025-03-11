using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessAgentsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly STTProviderManager _sttProviderManager;
        private readonly TTSProviderManager _ttsProviderManager;

        public AppUserBusinessAgentsController(
            UserManager userManager,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager
        )
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _llmProviderManager = llmProviderManager;
            _sttProviderManager = sttProviderManager;
            _ttsProviderManager = ttsProviderManager;
        }

        [HttpPost("/app/user/business/{businessId}/agents/save")]
        public async Task<FunctionReturnResult<BusinessAppAgent?>> SaveBusinessAgent(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppAgent?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessAgent:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessAgent:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessAgent:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessAgent:4";
                result.Message = "User does not have permission to edit businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessAgent:5";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessAgent:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessAgent:8";
                result.Message = "Business is currently disabled";

                if (businessResult.Data.Permission.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Agents.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessAgent:9";
                result.Message = "Business does not have permission to access agents";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Agents.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Agents.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessAgent:10";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("agentId", out StringValues existingAgentIdValue);
            string? existingAgentId = existingAgentIdValue.ToString();
            BusinessAppAgent? existingAgentData = null;

            if (postType == "new")
            {
                if (businessResult.Data.Permission.Agents.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessAgent:11";
                    result.Message = "Business does not have permission to add new agents";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Agents.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Agents.DisabledAddingReason;
                    }

                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (businessResult.Data.Permission.Agents.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessAgent:12";
                    result.Message = "Business does not have permission to edit agents";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Agents.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Agents.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingAgentId))
                {
                    result.Code = "SaveBusinessAgent:13";
                    result.Message = "Missing existing agent id";
                    return result;
                }

                existingAgentData = await _businessManager.GetAgentsManager().GetAgentById(businessId, existingAgentId);
                if (existingAgentData == null)
                {
                    result.Code = "SaveBusinessAgent:14";
                    result.Message = "Agent does not exist";
                    return result;
                }
            }
            
            var addOrUpdateResult = await _businessManager.GetAgentsManager().AddOrUpdateAgent(businessId, postType, formData, existingAgentData, _llmProviderManager, _sttProviderManager, _ttsProviderManager);
            if (!addOrUpdateResult.Success)
            {
                result.Code = "SaveBusinessAgent:" + addOrUpdateResult.Code;
                result.Message = addOrUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = addOrUpdateResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/agents/script/save")]
        public async Task<FunctionReturnResult<BusinessAppAgentScript?>> SaveBusinessAgentScript(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppAgentScript?>();

            // Session validation
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessAgentScript:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessAgentScript:2";
                result.Message = "Session validation failed";
                return result;
            }

            // User validation
            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessAgentScript:3";
                result.Message = "User not found";
                return result;
            }

            // User permissions
            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessAgentScript:4";
                result.Message = "User does not have permission to edit businesses";
                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }
                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessAgentScript:5";
                result.Message = "User does not own this business";
                return result;
            }

            // Business validation
            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessAgentScript:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            // Business permissions
            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessAgentScript:6";
                result.Message = "Business is currently disabled";
                if (businessResult.Data.Permission.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }
                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }
                return result;
            }

            // Agents Permissions
            if (businessResult.Data.Permission.Agents.DisabledFullAt != null || businessResult.Data.Permission.Agents.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessAgentScript:7";
                result.Message = "Business does not have permission to access agents";

                if (businessResult.Data.Permission.Agents.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Agents.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Agents.DisabledFullReason;
                }
                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Agents.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Agents.DisabledEditingReason;
                }

                return result;
            }

            // Post type validation
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveBusinessAgentScript:8";
                result.Message = "Invalid post type";
                return result;
            }

            // Agent validation
            formData.TryGetValue("agentId", out StringValues agentIdValue);
            string? agentId = agentIdValue.ToString();
            if (string.IsNullOrWhiteSpace(agentId))
            {
                result.Code = "SaveBusinessAgentScript:9";
                result.Message = "Agent ID is required";
                return result;
            }

            var agent = await _businessManager.GetAgentsManager().GetAgentById(businessId, agentId);
            if (agent == null)
            {
                result.Code = "SaveBusinessAgentScript:10";
                result.Message = "Agent not found";
                return result;
            }

            // Script validation for edit mode
            BusinessAppAgentScript? existingScriptData = null;
            if (postType == "edit")
            {
                formData.TryGetValue("scriptId", out StringValues scriptIdValue);
                string? scriptId = scriptIdValue.ToString();
                if (string.IsNullOrWhiteSpace(scriptId))
                {
                    result.Code = "SaveBusinessAgentScript:11";
                    result.Message = "Script ID is required for edit mode";
                    return result;
                }

                existingScriptData = agent.Scripts.FirstOrDefault(s => s.Id == scriptId);
                if (existingScriptData == null)
                {
                    result.Code = "SaveBusinessAgentScript:12";
                    result.Message = "Script not found";
                    return result;
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
                result.Code = "SaveBusinessAgentScript:" + addOrUpdateResult.Code;
                result.Message = addOrUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = addOrUpdateResult.Data;
            return result;
        }
    }

}

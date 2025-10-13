using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminLLMProviderController : Controller
    {
        private readonly LLMProviderManager _llmProviderManager;
        private readonly UserManager _userManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminLLMProviderController(LLMProviderManager llmProviderManager, UserManager userManager, IntegrationsManager integrationsManager)
        {
            _llmProviderManager = llmProviderManager;
            _userManager = userManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/llmproviders")]
        public async Task<FunctionReturnResult<List<LLMProviderData>?>> GetLLMProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LLMProviderData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetLLMProviders:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetLLMProviders:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetLLMProviders:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetLLMProviders:4";
                result.Message = "User is not an admin";
                return result;
            }

            var providersResult = await _llmProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                result.Code = "GetLLMProviders:" + providersResult.Code;
                result.Message = providersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = providersResult.Data;
            return result;
        }

        [HttpPost("/app/admin/llmproviders/save")]
        public async Task<FunctionReturnResult<LLMProviderData?>> SaveLLMProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLLMProvider:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLLMProvider:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLLMProvider:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLLMProvider:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveLLMProvider:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveLLMProvider:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
            if (provider == null)
            {
                result.Code = "SaveLLMProvider:7";
                result.Message = "Provider not found";
                return result;
            }

            var saveResult = await _llmProviderManager.UpdateProvider(provider, formData, _integrationsManager);
            if (!saveResult.Success)
            {
                result.Code = "SaveLLMProvider:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

        [HttpPost("/app/admin/llmproviders/model/save")]
        public async Task<FunctionReturnResult<LLMProviderModelData?>> SaveLLMProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderModelData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLLMProviderModel:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLLMProviderModel:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLLMProviderModel:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLLMProviderModel:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveLLMProviderModel:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveLLMProviderModel:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
            if (provider == null)
            {
                result.Code = "SaveLLMProviderModel:7";
                result.Message = "Provider not found";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                result.Code = "SaveLLMProviderModel:8";
                result.Message = "Post type is required or is not edit or new";
                return result;
            }

            string? modelId = formData["modelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                result.Code = "SaveLLMProviderModel:9";
                result.Message = "Model id is required";
                return result;
            }

            LLMProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
            if (postType == "edit")
            {
                if (oldModelData == null)
                {
                    result.Code = "SaveLLMProviderModel:10";
                    result.Message = "Model not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (oldModelData != null)
                {
                    result.Code = "SaveLLMProviderModel:11";
                    result.Message = "Model already exists";
                    return result;
                }
            }

            var saveResult = await _llmProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
            if (!saveResult.Success)
            {
                result.Code = "SaveLLMProviderModel:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

    }
}

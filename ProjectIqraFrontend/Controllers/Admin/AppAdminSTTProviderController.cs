using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.STT;
using IqraInfrastructure.Services.Integrations;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminSTTProviderController : Controller
    {
        private readonly STTProviderManager _sttProviderManager;
        private readonly UserManager _userManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminSTTProviderController(
            STTProviderManager sttProviderManager,
            UserManager userManager,
            IntegrationsManager integrationsManager)
        {
            _sttProviderManager = sttProviderManager;
            _userManager = userManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/sttproviders")]
        public async Task<FunctionReturnResult<List<STTProviderData>?>> GetSTTProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<STTProviderData>?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetSTTProviders:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetSTTProviders:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetSTTProviders:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetSTTProviders:4";
                result.Message = "User is not an admin";
                return result;
            }

            var providersResult = await _sttProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                result.Code = "GetSTTProviders:" + providersResult.Code;
                result.Message = providersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = providersResult.Data;
            return result;
        }

        [HttpPost("/app/admin/sttproviders/save")]
        public async Task<FunctionReturnResult<STTProviderData?>> SaveSTTProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveSTTProvider:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveSTTProvider:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveSTTProvider:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveSTTProvider:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveSTTProvider:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceSTTProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveSTTProvider:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            var provider = await _sttProviderManager.GetProviderData((InterfaceSTTProviderEnum)providerIdEnum);
            if (provider == null)
            {
                result.Code = "SaveSTTProvider:7";
                result.Message = "Provider not found";
                return result;
            }

            var saveResult = await _sttProviderManager.UpdateProvider(provider, formData, _integrationsManager);
            if (!saveResult.Success)
            {
                result.Code = "SaveSTTProvider:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

        [HttpPost("/app/admin/sttproviders/model/save")]
        public async Task<FunctionReturnResult<STTProviderModelData?>> SaveSTTProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderModelData?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveSTTProviderModel:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveSTTProviderModel:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveSTTProviderModel:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveSTTProviderModel:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveSTTProviderModel:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceSTTProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveSTTProviderModel:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            var provider = await _sttProviderManager.GetProviderData((InterfaceSTTProviderEnum)providerIdEnum);
            if (provider == null)
            {
                result.Code = "SaveSTTProviderModel:7";
                result.Message = "Provider not found";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                result.Code = "SaveSTTProviderModel:8";
                result.Message = "Post type is required and must be either 'edit' or 'new'";
                return result;
            }

            string? modelId = formData["modelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                result.Code = "SaveSTTProviderModel:9";
                result.Message = "Model id is required";
                return result;
            }

            var oldModelData = provider.Models.Find(m => m.Id == modelId);
            if (postType == "edit" && oldModelData == null)
            {
                result.Code = "SaveSTTProviderModel:10";
                result.Message = "Model not found";
                return result;
            }
            else if (postType == "new" && oldModelData != null)
            {
                result.Code = "SaveSTTProviderModel:11";
                result.Message = "Model already exists";
                return result;
            }

            var saveResult = await _sttProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
            if (!saveResult.Success)
            {
                result.Code = "SaveSTTProviderModel:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }
    }
}
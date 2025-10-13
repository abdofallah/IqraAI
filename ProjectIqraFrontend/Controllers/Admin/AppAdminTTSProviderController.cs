using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.TTS;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminTTSProviderController : Controller
    {
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly UserManager _userManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminTTSProviderController(
            TTSProviderManager ttsProviderManager,
            UserManager userManager,
            IntegrationsManager integrationsManager)
        {
            _ttsProviderManager = ttsProviderManager;
            _userManager = userManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/ttsproviders")]
        public async Task<FunctionReturnResult<List<TTSProviderData>?>> GetTTSProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<TTSProviderData>?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetTTSProviders:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetTTSProviders:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetTTSProviders:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetTTSProviders:4";
                result.Message = "User is not an admin";
                return result;
            }

            var providersResult = await _ttsProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                result.Code = "GetTTSProviders:" + providersResult.Code;
                result.Message = providersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = providersResult.Data;
            return result;
        }

        [HttpPost("/app/admin/ttsproviders/save")]
        public async Task<FunctionReturnResult<TTSProviderData?>> SaveTTSProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveTTSProvider:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveTTSProvider:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveTTSProvider:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveTTSProvider:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveTTSProvider:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceTTSProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveTTSProvider:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            var provider = await _ttsProviderManager.GetProviderData((InterfaceTTSProviderEnum)providerIdEnum);
            if (provider == null)
            {
                result.Code = "SaveTTSProvider:7";
                result.Message = "Provider not found";
                return result;
            }

            var saveResult = await _ttsProviderManager.UpdateProvider(provider, formData, _integrationsManager);
            if (!saveResult.Success)
            {
                result.Code = "SaveTTSProvider:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

        [HttpPost("/app/admin/ttsproviders/speaker/save")]
        public async Task<FunctionReturnResult<TTSProviderSpeakerData?>> SaveTTSProviderSpeaker(IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderSpeakerData?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveTTSProviderSpeaker:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveTTSProviderSpeaker:2";
                result.Message = "Session validation failed";
                return result;
            }

            var user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveTTSProviderSpeaker:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveTTSProviderSpeaker:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveTTSProviderSpeaker:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceTTSProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveTTSProviderSpeaker:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            var provider = await _ttsProviderManager.GetProviderData((InterfaceTTSProviderEnum)providerIdEnum);
            if (provider == null)
            {
                result.Code = "SaveTTSProviderSpeaker:7";
                result.Message = "Provider not found";
                return result;
            }

            string? speakerId = formData["speakerId"];
            if (string.IsNullOrEmpty(speakerId))
            {
                result.Code = "SaveTTSProviderSpeaker:8";
                result.Message = "Speaker id is required";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                result.Code = "SaveTTSProviderSpeaker:9";
                result.Message = "Post type is required and must be either 'edit' or 'new'";
                return result;
            }

            var oldSpeakerData = provider.Models.Find(s => s.Id == speakerId);
            if (postType == "edit" && oldSpeakerData == null)
            {
                result.Code = "SaveTTSProviderSpeaker:10";
                result.Message = "Speaker not found";
                return result;
            }
            else if (postType == "new" && oldSpeakerData != null)
            {
                result.Code = "SaveTTSProviderSpeaker:11";
                result.Message = "Speaker already exists";
                return result;
            }

            var saveResult = await _ttsProviderManager.AddUpdateProviderSpeaker(
                provider, speakerId, postType, oldSpeakerData, formData);

            if (!saveResult.Success)
            {
                result.Code = "SaveTTSProviderSpeaker:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }
    }
}
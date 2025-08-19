using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminEmbeddingProviderController : Controller
    {
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly UserManager _userManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminEmbeddingProviderController(EmbeddingProviderManager embeddingProviderManager, UserManager userManager, IntegrationsManager integrationsManager)
        {
            _embeddingProviderManager = embeddingProviderManager;
            _userManager = userManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/embeddingproviders")]
        public async Task<FunctionReturnResult<List<EmbeddingProviderData>?>> GetEmbeddingProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<EmbeddingProviderData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("GetEmbeddingProviders:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("GetEmbeddingProviders:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("GetEmbeddingProviders:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("GetEmbeddingProviders:4", "User is not an admin");
            }

            var providersResult = await _embeddingProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                return result.SetFailureResult("GetEmbeddingProviders:" + providersResult.Code, providersResult.Message);
            }

            return result.SetSuccessResult(providersResult.Data);
        }

        [HttpPost("/app/admin/embeddingproviders/save")]
        public async Task<FunctionReturnResult<EmbeddingProviderData?>> SaveEmbeddingProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("SaveEmbeddingProvider:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("SaveEmbeddingProvider:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("SaveEmbeddingProvider:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("SaveEmbeddingProvider:4", "User is not an admin");
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                return result.SetFailureResult("SaveEmbeddingProvider:5", "Provider id is required");
            }

            if (!Enum.TryParse(typeof(InterfaceEmbeddingProviderEnum), providerId, true, out object? providerIdEnum))
            {
                return result.SetFailureResult("SaveEmbeddingProvider:6", "Invalid provider id enum");
            }

            EmbeddingProviderData? provider = await _embeddingProviderManager.GetProviderData(((InterfaceEmbeddingProviderEnum)providerIdEnum));
            if (provider == null)
            {
                return result.SetFailureResult("SaveEmbeddingProvider:7", "Provider not found");
            }

            var saveResult = await _embeddingProviderManager.UpdateProvider(provider, formData, _integrationsManager);
            if (!saveResult.Success)
            {
                return result.SetFailureResult("SaveEmbeddingProvider:" + saveResult.Code, saveResult.Message);
            }

            return result.SetSuccessResult(saveResult.Data);
        }

        [HttpPost("/app/admin/embeddingproviders/model/save")] // Changed Route
        public async Task<FunctionReturnResult<EmbeddingProviderModelData?>> SaveEmbeddingProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderModelData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:4", "User is not an admin");
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:5", "Provider id is required");
            }

            if (!Enum.TryParse(typeof(InterfaceEmbeddingProviderEnum), providerId, true, out object? providerIdEnum))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:6", "Invalid provider id enum");
            }

            EmbeddingProviderData? provider = await _embeddingProviderManager.GetProviderData(((InterfaceEmbeddingProviderEnum)providerIdEnum));
            if (provider == null)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:7", "Provider not found");
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:8", "Post type is required or is not 'edit' or 'new'");
            }

            string? modelId = formData["modelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:9", "Model id is required");
            }

            EmbeddingProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
            if (postType == "edit" && oldModelData == null)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:10", "Model not found for editing");
            }
            else if (postType == "new" && oldModelData != null)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:11", "A model with this ID already exists");
            }

            var saveResult = await _embeddingProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
            if (!saveResult.Success)
            {
                return result.SetFailureResult("SaveEmbeddingProviderModel:" + saveResult.Code, saveResult.Message);
            }

            return result.SetSuccessResult(saveResult.Data);
        }
    }
}

using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Rerank;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminRerankProviderController : Controller
    {
        private readonly RerankProviderManager _rerankProviderManager;
        private readonly UserManager _userManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminRerankProviderController(RerankProviderManager rerankProviderManager, UserManager userManager, IntegrationsManager integrationsManager)
        {
            _rerankProviderManager = rerankProviderManager;
            _userManager = userManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/rerankproviders")]
        public async Task<FunctionReturnResult<List<RerankProviderData>?>> GetRerankProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<RerankProviderData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("GetRerankProviders:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("GetRerankProviders:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("GetRerankProviders:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("GetRerankProviders:4", "User is not an admin");
            }

            var providersResult = await _rerankProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                return result.SetFailureResult("GetRerankProviders:" + providersResult.Code, providersResult.Message);
            }

            return result.SetSuccessResult(providersResult.Data);
        }

        [HttpPost("/app/admin/rerankproviders/save")]
        public async Task<FunctionReturnResult<RerankProviderData?>> SaveRerankProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("SaveRerankProvider:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("SaveRerankProvider:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("SaveRerankProvider:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("SaveRerankProvider:4", "User is not an admin");
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                return result.SetFailureResult("SaveRerankProvider:5", "Provider id is required");
            }

            if (!Enum.TryParse(typeof(InterfaceRerankProviderEnum), providerId, true, out object? providerIdEnum))
            {
                return result.SetFailureResult("SaveRerankProvider:6", "Invalid provider id enum");
            }

            RerankProviderData? provider = await _rerankProviderManager.GetProviderData(((InterfaceRerankProviderEnum)providerIdEnum));
            if (provider == null)
            {
                return result.SetFailureResult("SaveRerankProvider:7", "Provider not found");
            }

            var saveResult = await _rerankProviderManager.UpdateProvider(provider, formData, _integrationsManager);
            if (!saveResult.Success)
            {
                return result.SetFailureResult("SaveRerankProvider:" + saveResult.Code, saveResult.Message);
            }

            return result.SetSuccessResult(saveResult.Data);
        }

        [HttpPost("/app/admin/rerankproviders/model/save")] // Changed Route
        public async Task<FunctionReturnResult<RerankProviderModelData?>> SaveRerankProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderModelData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("SaveRerankProviderModel:1", "Invalid session data");
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                return result.SetFailureResult("SaveRerankProviderModel:2", "Session validation failed");
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult("SaveRerankProviderModel:3", "User not found");
            }

            if (!user.Permission.IsAdmin)
            {
                return result.SetFailureResult("SaveRerankProviderModel:4", "User is not an admin");
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                return result.SetFailureResult("SaveRerankProviderModel:5", "Provider id is required");
            }

            if (!Enum.TryParse(typeof(InterfaceRerankProviderEnum), providerId, true, out object? providerIdEnum))
            {
                return result.SetFailureResult("SaveRerankProviderModel:6", "Invalid provider id enum");
            }

            RerankProviderData? provider = await _rerankProviderManager.GetProviderData(((InterfaceRerankProviderEnum)providerIdEnum));
            if (provider == null)
            {
                return result.SetFailureResult("SaveRerankProviderModel:7", "Provider not found");
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                return result.SetFailureResult("SaveRerankProviderModel:8", "Post type is required or is not 'edit' or 'new'");
            }

            string? modelId = formData["modelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                return result.SetFailureResult("SaveRerankProviderModel:9", "Model id is required");
            }

            RerankProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
            if (postType == "edit" && oldModelData == null)
            {
                return result.SetFailureResult("SaveRerankProviderModel:10", "Model not found for editing");
            }
            else if (postType == "new" && oldModelData != null)
            {
                return result.SetFailureResult("SaveRerankProviderModel:11", "A model with this ID already exists");
            }

            var saveResult = await _rerankProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
            if (!saveResult.Success)
            {
                return result.SetFailureResult("SaveRerankProviderModel:" + saveResult.Code, saveResult.Message);
            }

            return result.SetSuccessResult(saveResult.Data);
        }
    }
}

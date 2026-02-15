using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.TTS;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminTTSProviderController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly TTSProviderManager _ttsProviderManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminTTSProviderController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            TTSProviderManager ttsProviderManager,
            IntegrationsManager integrationsManager)
        {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _ttsProviderManager = ttsProviderManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/ttsproviders")]
        public async Task<FunctionReturnResult<List<TTSProviderData>?>> GetTTSProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<TTSProviderData>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetTTSProviders:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var providersResult = await _ttsProviderManager.GetProviderList(page, pageSize);
                if (!providersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetTTSProviders:" + providersResult.Code,
                        providersResult.Message
                    );
                }

                return result.SetSuccessResult(providersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTTSProviders:Exception",
                    $"Error getting TTS providers: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/ttsproviders/save")]
        public async Task<FunctionReturnResult<TTSProviderData?>> SaveTTSProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveTTSProvider:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveTTSProvider:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceTTSProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveTTSProvider:INVALID_PROVIDER_ID",
                        "Invalid provider id"
                    );
                }

                var provider = await _ttsProviderManager.GetProviderData((InterfaceTTSProviderEnum)providerIdEnum);
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveTTSProvider:NOT_FOUND",
                        "Provider not found"
                    );
                }

                var saveResult = await _ttsProviderManager.UpdateProvider(provider, formData, _integrationsManager);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveTTSProvider:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveTTSProvider:Exception",
                    $"Error saving TTS provider: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/ttsproviders/model/save")]
        public async Task<FunctionReturnResult<TTSProviderModelData?>> SaveTTSProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderModelData?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveTTSProviderModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceTTSProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:INVALID_PROVIDER_ID",
                        "Invalid provider id"
                    );
                }

                var provider = await _ttsProviderManager.GetProviderData((InterfaceTTSProviderEnum)providerIdEnum);
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:NOT_FOUND",
                        "Provider not found"
                    );
                }

                string? modelId = formData["modelId"];
                if (string.IsNullOrEmpty(modelId))
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:EMPTY_MODEL_ID",
                        "Model id is required"
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:INVALID_POST_TYPE",
                        "Post type is required and must be either 'edit' or 'new'"
                    );
                }

                var oldModelData = provider.Models.Find(s => s.Id == modelId);
                if (postType == "edit" && oldModelData == null)
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:NOT_FOUND",
                        "Model not found"
                    );
                }
                else if (postType == "new" && oldModelData != null)
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:MODEL_EXISTS",
                        "Model already exists with this id"
                    );
                }

                var saveResult = await _ttsProviderManager.AddUpdateProviderModel(
                    provider, modelId, postType, oldModelData, formData);

                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveTTSProviderModel:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveTTSProviderModel:Exception",
                    $"Error saving TTS provider model: {ex.Message}"
                );
            }
        }
    }
}
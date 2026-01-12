using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Embedding;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminEmbeddingProviderController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly EmbeddingProviderManager _embeddingProviderManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminEmbeddingProviderController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            EmbeddingProviderManager embeddingProviderManager,
            IntegrationsManager integrationsManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _embeddingProviderManager = embeddingProviderManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/embeddingproviders")]
        public async Task<FunctionReturnResult<List<EmbeddingProviderData>?>> GetEmbeddingProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<EmbeddingProviderData>?>();

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
                        $"GetEmbeddingProviders:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var providersResult = await _embeddingProviderManager.GetProviderList(page, pageSize);
                if (!providersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetEmbeddingProviders:" + providersResult.Code,
                        providersResult.Message
                    );
                }

                return result.SetSuccessResult(providersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetEmbeddingProviders:EXCEPTION",
                    $"Failed to get embedding providers. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/embeddingproviders/save")]
        public async Task<FunctionReturnResult<EmbeddingProviderData?>> SaveEmbeddingProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

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
                        $"SaveEmbeddingProvider:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProvider:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceEmbeddingProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProvider:INVALID_PROVIDER_ID_ENUM",
                        "Invalid provider id enum"
                    );
                }

                EmbeddingProviderData? provider = await _embeddingProviderManager.GetProviderData(((InterfaceEmbeddingProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProvider:NOT_FOUND",
                        "Provider not found"
                    );
                }

                var saveResult = await _embeddingProviderManager.UpdateProvider(provider, formData, _integrationsManager);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProvider:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveEmbeddingProvider:EXCEPTION",
                    $"Failed to save embedding provider. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/embeddingproviders/model/save")]
        public async Task<FunctionReturnResult<EmbeddingProviderModelData?>> SaveEmbeddingProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderModelData?>();

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
                        $"SaveEmbeddingProviderModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceEmbeddingProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:INVALID_PROVIDER_ID_ENUM",
                        "Invalid provider id enum"
                    );
                }

                EmbeddingProviderData? provider = await _embeddingProviderManager.GetProviderData(((InterfaceEmbeddingProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:NOT_FOUND",
                        "Provider not found"
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:INVALID_POST_TYPE",
                        "Post type is required or is not 'edit' or 'new'"
                    );
                }

                string? modelId = formData["modelId"];
                if (string.IsNullOrEmpty(modelId))
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:EMPTY_MODEL_ID",
                        "Model id is required"
                    );
                }

                EmbeddingProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
                if (postType == "edit" && oldModelData == null)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:MODEL_NOT_FOUND",
                        "Model not found for editing"
                    );
                }
                else if (postType == "new" && oldModelData != null)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:MODEL_ALREADY_EXISTS",
                        "A model with this ID already exists"
                    );
                }

                var saveResult = await _embeddingProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveEmbeddingProviderModel:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveEmbeddingProviderModel:EXCEPTION",
                    $"Failed to save embedding provider model. Exception: {ex.Message}"
                );
            }
        }
    }
}

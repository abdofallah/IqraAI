using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.LLM;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminLLMProviderController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminLLMProviderController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            LLMProviderManager llmProviderManager,
            IntegrationsManager integrationsManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _llmProviderManager = llmProviderManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/llmproviders")]
        public async Task<FunctionReturnResult<List<LLMProviderData>?>> GetLLMProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LLMProviderData>?>();

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
                        $"GetLLMProviders:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var providersResult = await _llmProviderManager.GetProviderList(page, pageSize);
                if (!providersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetLLMProviders:" + providersResult.Code,
                        providersResult.Message
                    );
                }

                return result.SetSuccessResult(providersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetLLMProviders:EXCEPTION",
                    $"Failed to get LLM providers. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/llmproviders/save")]
        public async Task<FunctionReturnResult<LLMProviderData?>> SaveLLMProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

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
                        $"SaveLLMProvider:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveLLMProvider:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveLLMProvider:INVALID_PROVIDER_ID",
                        "Invalid provider id enum"
                    );
                }

                LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveLLMProvider:PROVIDER_NOT_FOUND",
                        "Provider not found"
                    );
                }

                var saveResult = await _llmProviderManager.UpdateProvider(provider, formData, _integrationsManager);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveLLMProvider:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveLLMProvider:EXCEPTION",
                    $"Failed to save LLM provider. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/llmproviders/model/save")]
        public async Task<FunctionReturnResult<LLMProviderModelData?>> SaveLLMProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderModelData?>();

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
                        $"SaveLLMProviderModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:INVALID_PROVIDER_ID",
                        "Invalid provider id enum"
                    );
                }

                LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:PROVIDER_NOT_FOUND",
                        "Provider not found"
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                string? modelId = formData["modelId"];
                if (string.IsNullOrEmpty(modelId))
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:EMPTY_MODEL_ID",
                        "Model id is required"
                    );
                }

                LLMProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
                if (postType == "edit")
                {
                    if (oldModelData == null)
                    {
                        return result.SetFailureResult(
                            "SaveLLMProviderModel:MODEL_NOT_FOUND",
                            "Model not found"
                        );
                    }
                }
                else if (postType == "new")
                {
                    if (oldModelData != null)
                    {
                        return result.SetFailureResult(
                            "SaveLLMProviderModel:MODEL_ALREADY_EXISTS",
                            "Model already exists with id"
                        );
                    }
                }

                var saveResult = await _llmProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveLLMProviderModel:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveLLMProviderModel:EXCEPTION",
                    $"Failed to save LLM provider model. Exception: {ex.Message}"
                );
            }
        }
    }
}

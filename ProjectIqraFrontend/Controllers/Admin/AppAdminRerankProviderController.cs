using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Rerank;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminRerankProviderController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly RerankProviderManager _rerankProviderManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminRerankProviderController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            RerankProviderManager rerankProviderManager,
            IntegrationsManager integrationsManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _rerankProviderManager = rerankProviderManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/rerankproviders")]
        public async Task<FunctionReturnResult<List<RerankProviderData>?>> GetRerankProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<RerankProviderData>?>();

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
                        $"GetRerankProviders:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var providersResult = await _rerankProviderManager.GetProviderList(page, pageSize);
                if (!providersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetRerankProviders:" + providersResult.Code,
                        providersResult.Message
                    );
                }

                return result.SetSuccessResult(providersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetRerankProviders:Exception",
                    $"Error getting rerank providers: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/rerankproviders/save")]
        public async Task<FunctionReturnResult<RerankProviderData?>> SaveRerankProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

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
                        $"SaveRerankProvider:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveRerankProvider:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceRerankProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveRerankProvider:INVALID_PROVIDER_ID_ENUM",
                        "Invalid provider id enum"
                    );
                }

                RerankProviderData? provider = await _rerankProviderManager.GetProviderData(((InterfaceRerankProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveRerankProvider:PROVIDER_NOT_FOUND",
                        "Provider not found"
                    );
                }

                var saveResult = await _rerankProviderManager.UpdateProvider(provider, formData, _integrationsManager);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveRerankProvider:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveRerankProvider:Exception",
                    $"Error saving rerank provider: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/rerankproviders/model/save")] // Changed Route
        public async Task<FunctionReturnResult<RerankProviderModelData?>> SaveRerankProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderModelData?>();

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
                        $"SaveRerankProviderModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceRerankProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:INVALID_PROVIDER_ID_ENUM",
                        "Invalid provider id enum"
                    );
                }

                RerankProviderData? provider = await _rerankProviderManager.GetProviderData(((InterfaceRerankProviderEnum)providerIdEnum));
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:NOT_FOUND",
                        "Provider not found"
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:INVALID_POST_TYPE",
                        "Post type is required or is not 'edit' or 'new'"
                    );
                }

                string? modelId = formData["modelId"];
                if (string.IsNullOrEmpty(modelId))
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:MISSING_MODEL_ID",
                        "Model id is required"
                    );
                }

                RerankProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
                if (postType == "edit" && oldModelData == null)
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:MODEL_NOT_FOUND",
                        "Model not found for editing"
                    );
                }
                else if (postType == "new" && oldModelData != null)
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:ALREADY_EXISTS",
                        "A model with this ID already exists"
                    );
                }

                var saveResult = await _rerankProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveRerankProviderModel:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveRerankProviderModel:Exception",
                    $"Error saving rerank provider model: {ex.Message}"
                );
            }
        }
    }
}

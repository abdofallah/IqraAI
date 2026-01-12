using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.STT;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.STT;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminSTTProviderController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly STTProviderManager _sttProviderManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppAdminSTTProviderController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            STTProviderManager sttProviderManager,
            IntegrationsManager integrationsManager)
        {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _sttProviderManager = sttProviderManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/admin/sttproviders")]
        public async Task<FunctionReturnResult<List<STTProviderData>?>> GetSTTProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<STTProviderData>?>();

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
                        $"GetSTTProviders:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var providersResult = await _sttProviderManager.GetProviderList(page, pageSize);
                if (!providersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetSTTProviders:" + providersResult.Code,
                        providersResult.Message
                    );
                }

                return result.SetSuccessResult(providersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetSTTProviders:Exception",
                    $"An error occurred: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/sttproviders/save")]
        public async Task<FunctionReturnResult<STTProviderData?>> SaveSTTProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

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
                        $"SaveSTTProvider:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveSTTProvider:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceSTTProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveSTTProvider:INVALID_PROVIDER_ID",
                        "Invalid provider id enum"
                    );
                }

                var provider = await _sttProviderManager.GetProviderData((InterfaceSTTProviderEnum)providerIdEnum);
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveSTTProvider:NOT_FOUND",
                        "Provider not found"
                    );
                }

                var saveResult = await _sttProviderManager.UpdateProvider(provider, formData, _integrationsManager);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveSTTProvider:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveSTTProvider:Exception",
                    $"An error occurred: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/sttproviders/model/save")]
        public async Task<FunctionReturnResult<STTProviderModelData?>> SaveSTTProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderModelData?>();

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
                        $"SaveSTTProviderModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? providerId = formData["providerId"];
                if (string.IsNullOrEmpty(providerId))
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:EMPTY_PROVIDER_ID",
                        "Provider id is required"
                    );
                }

                if (!Enum.TryParse(typeof(InterfaceSTTProviderEnum), providerId, true, out object? providerIdEnum))
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:INVALID_PROVIDER_ID",
                        "Invalid provider id enum"
                    );
                }

                var provider = await _sttProviderManager.GetProviderData((InterfaceSTTProviderEnum)providerIdEnum);
                if (provider == null)
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:NOT_FOUND",
                        "Provider not found"
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:INVALID_POST_TYPE",
                        "Post type is required and must be either 'edit' or 'new'"
                    );
                }

                string? modelId = formData["modelId"];
                if (string.IsNullOrEmpty(modelId))
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:EMPTY_MODEL_ID",
                        "Model id is required"
                    );
                }

                var oldModelData = provider.Models.Find(m => m.Id == modelId);
                if (postType == "edit" && oldModelData == null)
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:MODEL_NOT_FOUND",
                        "Model not found"
                    );
                }
                else if (postType == "new" && oldModelData != null)
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:MODEL_ALREADY_EXISTS",
                        "Model already exists with id"
                    );
                }

                var saveResult = await _sttProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
                if (!saveResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveSTTProviderModel:" + saveResult.Code,
                        saveResult.Message
                    );
                }

                return result.SetSuccessResult(saveResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveSTTProviderModel:Exception",
                    $"An error occurred: {ex.Message}"
                );
            }
        }
    }
}
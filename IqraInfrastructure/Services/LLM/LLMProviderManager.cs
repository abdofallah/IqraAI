using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Repositories.LLM;
using System.Reflection;

namespace IqraInfrastructure.Services.LLM
{
    public class LLMProviderManager
    {
        private readonly LLMProviderRepository _llmProviderRepository;
        private Dictionary<InterfaceLLMProviderEnum, Type> _llmProviderClasses = new Dictionary<InterfaceLLMProviderEnum, Type>();

        public LLMProviderManager(LLMProviderRepository llmProviderRepository)
        {
            _llmProviderRepository = llmProviderRepository;
        }

        public async Task InitializeProvidersAsync()
        {
            foreach (InterfaceLLMProviderEnum providerEnum in Enum.GetValues(typeof(InterfaceLLMProviderEnum)))
            {
                if (providerEnum == InterfaceLLMProviderEnum.Unknown)
                    continue;

                var provider = await _llmProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    await AddProvider(new LLMProviderData
                    {
                        Id = providerEnum,
                        DisabledAt = DateTime.UtcNow
                    });
                }
                else if (provider.DisabledAt == null)
                {
                    RegisterProviderService(providerEnum);
                }
            }
        }

        private void RegisterProviderService(InterfaceLLMProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var aiServiceType = typeof(IAIService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => aiServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderType", BindingFlags.Public | BindingFlags.Static);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceLLMProviderEnum)getProviderTypeMethod.Invoke(null, null);
                    if (returnedProviderEnum == providerEnum)
                    {
                        _llmProviderClasses[providerEnum] = type;
                        return;
                    }
                }
            }

            throw new Exception($"No matching IAIService implementation found for provider: {providerEnum}");
        }

        public async Task<FunctionReturnResult<LLMProviderData>> AddProvider(LLMProviderData providerData)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            if (providerData.Id == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "AddProvider:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var existingProvider = await _llmProviderRepository.GetProviderAsync(providerData.Id);
            if (existingProvider != null)
            {
                result.Code = "AddProvider:2";
                result.Message = "Provider already exists";
                return result;
            }

            providerData.DisabledAt = DateTime.UtcNow;

            await _llmProviderRepository.AddProviderAsync(providerData);

            result.Success = true;
            result.Data = providerData;
            return result;
        }

        public async Task<FunctionReturnResult<bool>> DisableProvider(InterfaceLLMProviderEnum providerId)
        {
            var result = new FunctionReturnResult<bool>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "DisableProvider:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.DisableProviderAsync(providerId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "DisableProvider:2";
                result.Message = "Provider not found or already disabled";
                return result;
            }

            _llmProviderClasses.Remove(providerId);

            result.Success = true;
            result.Data = true;
            return result;
        }

        public async Task<FunctionReturnResult<bool>> EnableProvider(InterfaceLLMProviderEnum providerId)
        {
            var result = new FunctionReturnResult<bool>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "EnableProvider:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.EnableProviderAsync(providerId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "EnableProvider:2";
                result.Message = "Provider not found or already enabled";
                return result;
            }

            try
            {
                RegisterProviderService(providerId);
            }
            catch (Exception ex)
            {
                result.Code = "EnableProvider:3";
                result.Message = $"Failed to register provider service: {ex.Message}";
                return result;
            }

            result.Success = true;
            result.Data = true;
            return result;
        }

        public async Task<FunctionReturnResult<LLMProviderData>> AddModel(InterfaceLLMProviderEnum providerId, LLMProviderModelData modelData)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "AddModel:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.AddModelAsync(providerId, modelData);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "AddModel:2";
                result.Message = "Provider not found or model already exists";
                return result;
            }

            result.Success = true;
            result.Data = await _llmProviderRepository.GetProviderAsync(providerId);
            return result;
        }

        public async Task<FunctionReturnResult<LLMProviderData>> UpdateModel(InterfaceLLMProviderEnum providerId, LLMProviderModelData modelData)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "UpdateModel:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.UpdateModelAsync(providerId, modelData);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "UpdateModel:2";
                result.Message = "Provider or model not found";
                return result;
            }

            result.Success = true;
            result.Data = await _llmProviderRepository.GetProviderAsync(providerId);
            return result;
        }

        public async Task<FunctionReturnResult<LLMProviderData>> DisableModel(InterfaceLLMProviderEnum providerId, string modelId)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "DisableModel:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.DisableModelAsync(providerId, modelId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "DisableModel:2";
                result.Message = "Provider or model not found";
                return result;
            }

            result.Success = true;
            result.Data = await _llmProviderRepository.GetProviderAsync(providerId);
            return result;
        }

        public async Task<FunctionReturnResult<LLMProviderData>> RemoveModel(InterfaceLLMProviderEnum providerId, string modelId)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "RemoveModel:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.RemoveModelAsync(providerId, modelId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "RemoveModel:2";
                result.Message = "Provider or model not found";
                return result;
            }

            result.Success = true;
            result.Data = await _llmProviderRepository.GetProviderAsync(providerId);
            return result;
        }

        public Type? GetProviderService(InterfaceLLMProviderEnum providerId)
        {
            if (_llmProviderClasses.TryGetValue(providerId, out var service))
            {
                return service;
            }
            return null;
        }
    }
}

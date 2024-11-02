using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using IqraCore.Interfaces.AI;
using IqraCore.Utilities;
using IqraInfrastructure.Repositories.LLM;
using IqraInfrastructure.Services.Languages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Services.LLM
{
    public class LLMProviderManager
    {
        private readonly LLMProviderRepository _llmProviderRepository;
        private readonly LanguagesManager _languagesManager;

        private Dictionary<InterfaceLLMProviderEnum, Type> _llmProviderClasses = new Dictionary<InterfaceLLMProviderEnum, Type>();

        public LLMProviderManager(LLMProviderRepository llmProviderRepository, LanguagesManager languagesManager)
        {
            _llmProviderRepository = llmProviderRepository;
            _languagesManager = languagesManager;
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

        public async Task<FunctionReturnResult<List<LLMProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<LLMProviderData>?>();

            var providerList = await _llmProviderRepository.GetProviderListAsync(page, pageSize);
            if (providerList == null)
            {
                result.Code = "GetProviderList:1";
                result.Message = "No providers found";
                return result;
            }

            result.Success = true;
            result.Data = providerList;
            return result;
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

        public async Task<FunctionReturnResult<bool>> AddModel(InterfaceLLMProviderEnum providerId, LLMProviderModelData modelData)
        {
            var result = new FunctionReturnResult<bool>();

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
            return result;
        }

        public async Task<FunctionReturnResult<bool>> UpdateModelPromptTemplates(InterfaceLLMProviderEnum providerId, string modelId, Dictionary<string, string> promptTemplates)
        {
            var result = new FunctionReturnResult<bool>();

            if (providerId == InterfaceLLMProviderEnum.Unknown)
            {
                result.Code = "UpdateModelPromptTemplates:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var updateResult = await _llmProviderRepository.UpdateModelPromptTemplatesAsync(providerId, modelId, promptTemplates);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                result.Code = "UpdateModelPromptTemplates:2";
                result.Message = "Provider or model not found";
                return result;
            }

            result.Success = true;
            result.Data = true;
            return result;
        }

        public async Task<FunctionReturnResult<bool>> DisableModel(InterfaceLLMProviderEnum providerId, string modelId)
        {
            var result = new FunctionReturnResult<bool>();

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

        public async Task<LLMProviderData?> GetProviderData(InterfaceLLMProviderEnum providerId)
        {
            return await _llmProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<LLMProviderModelData?>> AddUpdateProviderModel(LLMProviderData provider, string modelId, string postType, LLMProviderModelData? oldModelData, IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderModelData?>();

            var newModelData = new LLMProviderModelData()
            {
                Id = modelId
            };

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddUpdateProviderModel:1";
                result.Message = "Changes data not found";
                return result;
            }

            if (string.IsNullOrEmpty(changesJsonString))
            {
                result.Code = "AddUpdateProviderModel:2";
                result.Message = "Changes data is empty";
                return result;
            }

            JsonDocument changesJsonElement;
            try
            {
                changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString);
            }
            catch (JsonException)
            {
                result.Code = "AddUpdateProviderModel:3";
                result.Message = "Invalid JSON format for changes";
                return result;
            }

            if (changesJsonElement == null)
            {
                result.Code = "AddUpdateProviderModel:3";
                result.Message = "Failed to parse changes JSON";
                return result;
            }

            // Model Name
            if (!changesJsonElement.RootElement.TryGetProperty("name", out var modelNameElement))
            {
                result.Code = "AddUpdateProviderModel:4";
                result.Message = "Model name not found";
                return result;
            }
            string? modelName = modelNameElement.GetString();
            if (string.IsNullOrEmpty(modelName))
            {
                result.Code = "AddUpdateProviderModel:5";
                result.Message = "Model name is empty";
                return result;
            }
            newModelData.Name = modelName;

            // Disabled
            if (changesJsonElement.RootElement.TryGetProperty("disabled", out var disabledElement))
            {
                bool isDisabled = disabledElement.GetBoolean();
                if (isDisabled)
                {
                    if (postType == "edit" && oldModelData?.DisabledAt != null)
                    {
                        newModelData.DisabledAt = oldModelData.DisabledAt;
                    }
                    else
                    {
                        newModelData.DisabledAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    newModelData.DisabledAt = null;
                }
            }

            // Input Price
            if (changesJsonElement.RootElement.TryGetProperty("inputPrice", out var inputPriceElement))
            {
                if (decimal.TryParse(inputPriceElement.GetString(), out decimal inputPrice))
                {
                    newModelData.InputPrice = inputPrice;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:6";
                    result.Message = "Invalid input price";
                    return result;
                }
            }

            // Input Price Token Unit
            if (changesJsonElement.RootElement.TryGetProperty("inputPriceTokenUnit", out var inputPriceTokenUnitElement))
            {
                if (int.TryParse(inputPriceTokenUnitElement.GetString(), out int inputPriceTokenUnit))
                {
                    newModelData.InputPriceTokenUnit = inputPriceTokenUnit;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:7";
                    result.Message = "Invalid input price token unit";
                    return result;
                }
            }

            // Output Price
            if (changesJsonElement.RootElement.TryGetProperty("outputPrice", out var outputPriceElement))
            {
                if (decimal.TryParse(outputPriceElement.GetString(), out decimal outputPrice))
                {
                    newModelData.OutputPrice = outputPrice;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:8";
                    result.Message = "Invalid output price";
                    return result;
                }
            }

            // Output Price Token Unit
            if (changesJsonElement.RootElement.TryGetProperty("outputPriceTokenUnit", out var outputPriceTokenUnitElement))
            {
                if (int.TryParse(outputPriceTokenUnitElement.GetString(), out int outputPriceTokenUnit))
                {
                    newModelData.OutputPriceTokenUnit = outputPriceTokenUnit;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:9";
                    result.Message = "Invalid output price token unit";
                    return result;
                }
            }

            // Max Input Token Length
            if (changesJsonElement.RootElement.TryGetProperty("maxInputTokenLength", out var maxInputTokenLengthElement))
            {
                if (int.TryParse(maxInputTokenLengthElement.GetString(), out int maxInputTokenLength))
                {
                    newModelData.MaxInputTokenLength = maxInputTokenLength;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:10";
                    result.Message = "Invalid max input token length";
                    return result;
                }
            }

            // Max Output Token Length
            if (changesJsonElement.RootElement.TryGetProperty("maxOutputTokenLength", out var maxOutputTokenLengthElement))
            {
                if (int.TryParse(maxOutputTokenLengthElement.GetString(), out int maxOutputTokenLength))
                {
                    newModelData.MaxOutputTokenLength = maxOutputTokenLength;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:11";
                    result.Message = "Invalid max output token length";
                    return result;
                }
            }

            // Prompts
            var appLanguages = await _languagesManager.GetAllLanguagesList();
            var promptValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                appLanguages.Data,
                changesJsonElement.RootElement,
                "promptTemplates",
                newModelData.PromptTemplates
            );

            if (!promptValidationResult.Success)
            {
                result.Code = "AddUpdateProviderModel:" + promptValidationResult.Code;
                result.Message = promptValidationResult.Message;
                return result;
            }

            // Saving new data to database
            if (postType == "new")
            {
                var addResult = await _llmProviderRepository.AddModelAsync(provider.Id, newModelData);
                if (!addResult.IsAcknowledged || addResult.ModifiedCount == 0)
                {
                    result.Code = "AddUpdateProviderModel:13";
                    result.Message = "Failed to add model";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                var editResult = await _llmProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                if (!editResult.IsAcknowledged || editResult.ModifiedCount == 0)
                {
                    result.Code = "AddUpdateProviderModel:14";
                    result.Message = "Failed to edit model";
                    return result;
                }
            }

            result.Data = newModelData;
            result.Success = true;
            return result;
        }

        public async Task<FunctionReturnResult<LLMProviderData?>> UpdateProvider(LLMProviderData provider, IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            // todo

            return result;
        }
    }
}

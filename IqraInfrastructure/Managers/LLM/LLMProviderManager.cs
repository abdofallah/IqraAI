using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.LLM;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Provider;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM.Providers;
using IqraInfrastructure.Repositories.LLM;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Managers.LLM
{
    public class LLMProviderManager
    {
        private readonly ILogger<LLMProviderManager> _logger;
        private readonly LLMProviderRepository _llmProviderRepository;
        private readonly LanguagesManager _languagesManager;
        private readonly IntegrationsManager _integrationsManager;

        private Dictionary<InterfaceLLMProviderEnum, Type> _llmProviderClasses = new Dictionary<InterfaceLLMProviderEnum, Type>();

        public LLMProviderManager(
            ILoggerFactory loggerFactory,
            LLMProviderRepository llmProviderRepository,
            LanguagesManager languagesManager,
            IntegrationsManager integrationsManager)
        {
            _logger = loggerFactory.CreateLogger<LLMProviderManager>();
            _llmProviderRepository = llmProviderRepository;
            _languagesManager = languagesManager;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeProvidersAsync()
        {
            var allEnums = Enum.GetValues(typeof(InterfaceLLMProviderEnum));
            foreach (InterfaceLLMProviderEnum providerEnum in allEnums)
            {
                if (providerEnum == InterfaceLLMProviderEnum.Unknown)
                    continue;

                var provider = await _llmProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    var addResult = await AddProvider(providerEnum);
                    if (!addResult.Success)
                    {
                        throw new Exception($"Failed to add llm provider: {providerEnum}: [{addResult.Code}] {addResult.Message}");
                    }
                }

                RegisterProviderService(providerEnum);
            }
        }

        private void RegisterProviderService(InterfaceLLMProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var aiServiceType = typeof(ILLMService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => aiServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderTypeStatic", BindingFlags.Static | BindingFlags.Public);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceLLMProviderEnum)getProviderTypeMethod.Invoke(null, null)!;
                    if (returnedProviderEnum == providerEnum)
                    {
                        _llmProviderClasses[providerEnum] = type;
                        return;
                    }
                }
                else
                {
                    throw new Exception($"No GetProviderTypeStatic method found in type: {type.Name}");
                }
            }

            throw new Exception($"No matching ILLMService implementation found for provider: {providerEnum}");
        }

        public async Task<FunctionReturnResult<List<LLMProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<LLMProviderData>?>();

            try
            {
                var providerList = await _llmProviderRepository.GetProviderListAsync(page, pageSize);
                if (providerList == null)
                {
                    return result.SetFailureResult(
                        "GetProviderList:NOT_FOUND",
                        "No providers found"
                    );
                }

                return result.SetSuccessResult(providerList);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetProviderList:EXCEPTION",
                    $"Failed to get provider list: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<LLMProviderData>> AddProvider(InterfaceLLMProviderEnum providerId)
        {
            var result = new FunctionReturnResult<LLMProviderData>();

            try
            {
                var providerData = new LLMProviderData()
                {
                    Id = providerId,
                    DisabledAt = DateTime.UtcNow
                };

                if (providerData.Id == InterfaceLLMProviderEnum.Unknown)
                {
                    return result.SetFailureResult(
                        "AddProvider:INVALID_ID",
                        "Invalid provider ID"
                    );
                }

                var existingProvider = await _llmProviderRepository.GetProviderAsync(providerData.Id);
                if (existingProvider != null)
                {
                    return result.SetFailureResult(
                        "AddProvider:EXISTS",
                        "Provider already exists"
                    );
                }

                var success = await _llmProviderRepository.AddProviderAsync(providerData);
                if (!success)
                {
                    return result.SetFailureResult(
                        "AddProvider:FAILED",
                        "Failed to add provider to database"
                    );
                }

                return result.SetSuccessResult(providerData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddProvider:EXCEPTION",
                    $"Failed to add provider: {ex.Message}"
                );
            }
        }

        public Type? GetProviderService(InterfaceLLMProviderEnum providerId)
        {
            return _llmProviderClasses.TryGetValue(providerId, out var service) ? service : null;
        }

        public async Task<LLMProviderData?> GetProviderData(InterfaceLLMProviderEnum providerId)
        {
            return await _llmProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<LLMProviderModelData?>> AddUpdateProviderModel(
            LLMProviderData provider,
            string modelId,
            string postType,
            LLMProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderModelData?>();

            try
            {
                var newModelData = new LLMProviderModelData()
                {
                    Id = modelId
                };

                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:CHANGES_DATA_NOT_FOUND",
                        "Changes data not found"
                    );
                }

                JsonDocument? changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString.ToString());
                if (changesJsonElement == null)
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:JSON_PARSE_ERROR",
                        "Failed to parse changes JSON"
                    );
                }

                var root = changesJsonElement.RootElement;

                // Model Name
                if (root.TryGetProperty("name", out var modelNameElement))
                {
                    string? modelName = modelNameElement.GetString();
                    if (string.IsNullOrEmpty(modelName))
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:EMPTY_NAME",
                            "Model name is empty"
                        );
                    }
                    newModelData.Name = modelName;
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:NAME_NOT_FOUND",
                        "Model name not found"
                    );
                }

                // Disabled State
                if (root.TryGetProperty("disabled", out var disabledElement))
                {
                    bool isDisabled = disabledElement.GetBoolean();
                    if (isDisabled)
                    {
                        newModelData.DisabledAt = (postType == "edit" && oldModelData?.DisabledAt != null)
                            ? oldModelData.DisabledAt
                            : DateTime.UtcNow;
                    }
                    else
                    {
                        newModelData.DisabledAt = null;
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:DISABLED_STATE_NOT_FOUND",
                        "Disabled state not found"
                    );
                }

                // Input Price
                if (root.TryGetProperty("inputPrice", out var inputPriceElement))
                {
                    if (decimal.TryParse(inputPriceElement.GetString(), out decimal inputPrice))
                    {
                        newModelData.InputPrice = inputPrice;
                    }
                    else if (inputPriceElement.TryGetDecimal(out decimal decimalInput))
                    {
                        newModelData.InputPrice = decimalInput;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_INPUT_PRICE",
                            "Invalid input price"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:INPUT_PRICE_NOT_FOUND",
                        "Input price not found"
                    );
                }

                // Input Price Token Unit
                if (root.TryGetProperty("inputPriceTokenUnit", out var inputPriceTokenUnitElement))
                {
                    if (int.TryParse(inputPriceTokenUnitElement.GetString(), out int inputPriceTokenUnit))
                    {
                        newModelData.InputPriceTokenUnit = inputPriceTokenUnit;
                    }
                    else if (inputPriceTokenUnitElement.TryGetInt32(out int intInputToken))
                    {
                        newModelData.InputPriceTokenUnit = intInputToken;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_INPUT_TOKEN_UNIT",
                            "Invalid input price token unit"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:INPUT_TOKEN_UNIT_NOT_FOUND",
                        "Input price token unit not found"
                    );
                }

                // Output Price
                if (root.TryGetProperty("outputPrice", out var outputPriceElement))
                {
                    if (decimal.TryParse(outputPriceElement.GetString(), out decimal outputPrice))
                    {
                        newModelData.OutputPrice = outputPrice;
                    }
                    else if (outputPriceElement.TryGetDecimal(out decimal decimalOutput))
                    {
                        newModelData.OutputPrice = decimalOutput;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_OUTPUT_PRICE",
                            "Invalid output price"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:OUTPUT_PRICE_NOT_FOUND",
                        "Output price not found"
                    );
                }

                // Output Price Token Unit
                if (root.TryGetProperty("outputPriceTokenUnit", out var outputPriceTokenUnitElement))
                {
                    if (int.TryParse(outputPriceTokenUnitElement.GetString(), out int outputPriceTokenUnit))
                    {
                        newModelData.OutputPriceTokenUnit = outputPriceTokenUnit;
                    }
                    else if (outputPriceTokenUnitElement.TryGetInt32(out int intOutputToken))
                    {
                        newModelData.OutputPriceTokenUnit = intOutputToken;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_OUTPUT_TOKEN_UNIT",
                            "Invalid output price token unit"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:OUTPUT_TOKEN_UNIT_NOT_FOUND",
                        "Output price token unit not found"
                    );
                }

                // Max Input Token Length
                if (root.TryGetProperty("maxInputTokenLength", out var maxInputTokenLengthElement))
                {
                    if (int.TryParse(maxInputTokenLengthElement.GetString(), out int maxInputTokenLength))
                    {
                        newModelData.MaxInputTokenLength = maxInputTokenLength;
                    }
                    else if (maxInputTokenLengthElement.TryGetInt32(out int intMaxInput))
                    {
                        newModelData.MaxInputTokenLength = intMaxInput;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_MAX_INPUT_LENGTH",
                            "Invalid max input token length"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:MAX_INPUT_LENGTH_NOT_FOUND",
                        "Max input token length not found"
                    );
                }

                // Max Output Token Length
                if (root.TryGetProperty("maxOutputTokenLength", out var maxOutputTokenLengthElement))
                {
                    if (int.TryParse(maxOutputTokenLengthElement.GetString(), out int maxOutputTokenLength))
                    {
                        newModelData.MaxOutputTokenLength = maxOutputTokenLength;
                    }
                    else if (maxOutputTokenLengthElement.TryGetInt32(out int intMaxOutput))
                    {
                        newModelData.MaxOutputTokenLength = intMaxOutput;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_MAX_OUTPUT_LENGTH",
                            "Invalid max output token length"
                        );
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:MAX_OUTPUT_LENGTH_NOT_FOUND",
                        "Max output token length not found"
                    );
                }

                // Save to database
                bool updateSuccess;
                if (postType == "new")
                {
                    var addResult = await _llmProviderRepository.AddModelAsync(provider.Id, newModelData);
                    updateSuccess = addResult.IsAcknowledged && addResult.ModifiedCount > 0;
                }
                else
                {
                    var updateResult = await _llmProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                    updateSuccess = updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;
                }

                if (!updateSuccess)
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:DB_UPDATE_FAILED",
                        $"Failed to {postType} model in database"
                    );
                }

                return result.SetSuccessResult(newModelData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddUpdateProviderModel:EXCEPTION",
                    $"Error processing model data: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<LLMProviderData?>> UpdateProvider(
            LLMProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:CHANGES_DATA_NOT_FOUND",
                        "Changes data not found"
                    );
                }

                JsonDocument? changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString.ToString());
                if (changesJsonElement == null)
                {
                    return result.SetFailureResult(
                        "UpdateProvider:JSON_PARSE_ERROR",
                        "Unable to parse changes json string"
                    );
                }

                var root = changesJsonElement.RootElement;
                var newProviderData = new LLMProviderData
                {
                    Id = provider.Id,
                    Models = provider.Models
                };

                // Handle disabled state
                if (root.TryGetProperty("disabled", out var disabledElement))
                {
                    bool disabled = disabledElement.GetBoolean();
                    newProviderData.DisabledAt = disabled ? (provider.DisabledAt ?? DateTime.UtcNow) : null;
                }
                else
                {
                    return result.SetFailureResult(
                        "UpdateProvider:DISABLED_STATE_NOT_FOUND",
                        "Provider disabled state not found"
                    );
                }

                // Handle integration selection
                if (root.TryGetProperty("integrationId", out var integrationIdElement))
                {
                    string? integrationId = integrationIdElement.GetString();

                    if (string.IsNullOrEmpty(integrationId))
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:MISSING_INTEGRATION_ID",
                            "Integration ID is required"
                        );
                    }

                    // Validate integration exists and is LLM type
                    var integration = await integrationsManager.getIntegrationData(integrationId);
                    if (integration.Data == null || !integration.Success)
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:SELECTED_INTEGRATION_NOT_FOUND",
                            "Selected integration not found"
                        );
                    }

                    if (!integration.Data.Type.Contains("LLM"))
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:INVALID_INTEGRATION",
                            "Selected integration is not an LLM integration"
                        );
                    }

                    newProviderData.IntegrationId = integrationId;
                }
                else
                {
                    return result.SetFailureResult(
                        "UpdateProvider:INTEGRATION_ID_NOT_FOUND",
                        "Integration ID not found in changes"
                    );
                }

                // Handle integration fields
                if (root.TryGetProperty("userIntegrationFields", out var fieldsElement))
                {
                    var availableModelIds = newProviderData.Models.Select(m => m.Id).ToList();

                    // Use the centralized helper for parsing and validation
                    var parseResult = ProviderIntegrationFieldsHelper.ParseAndValidateFields(fieldsElement, availableModelIds);

                    if (!parseResult.Success || parseResult.Data == null)
                    {
                        return result.SetFailureResult(
                            $"UpdateProvider:{parseResult.Code}",
                            parseResult.Message
                        );
                    }

                    newProviderData.UserIntegrationFields = parseResult.Data;
                }
                else
                {
                    return result.SetFailureResult(
                        "UpdateProvider:USER_INTEGRATION_FIELDS_NOT_FOUND",
                        "User integration fields not found"
                    );
                }

                // Save to database
                var updateResult = await _llmProviderRepository.UpdateProviderAsync(newProviderData);
                if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
                {
                    return result.SetFailureResult(
                        "UpdateProvider:UPDATE_FAILED",
                        "Failed to update provider"
                    );
                }

                return result.SetSuccessResult(newProviderData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "UpdateProvider:EXCEPTION",
                    $"Error processing provider update: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<LLMProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            try
            {
                var providerData = await _llmProviderRepository.GetProviderDataByIntegration(integrationType);

                if (providerData == null)
                {
                    return result.SetFailureResult(
                        "GetProviderDataByIntegration:NOT_FOUND",
                        "Provider not found by integration type"
                    );
                }

                return result.SetSuccessResult(providerData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetProviderDataByIntegration:EXCEPTION",
                    $"Failed to get provider data: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<ILLMService?>> BuildProviderServiceByIntegration(
            ILoggerFactory loggerFactory,
            BusinessAppIntegration integrationData,
            BusinessAppAgentIntegrationData agentIntegrationData,
            Dictionary<string, string> metaData)
        {
            var result = new FunctionReturnResult<ILLMService?>();

            try
            {
                var llmProviderData = await GetProviderDataByIntegration(integrationData.Type);
                if (!llmProviderData.Success || llmProviderData.Data == null)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:PROVIDER_NOT_FOUND",
                        "Provider not found by integration type"
                    );
                }

                // --- Helper functions for safe extraction ---
                string GetString(string key, string defaultValue = "")
                {
                    return agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null
                        ? val.ToString()! : defaultValue;
                }
                // ---------------------------------------------

                switch (llmProviderData.Data.Id)
                {
                    case InterfaceLLMProviderEnum.AnthropicClaude:
                        {
                            var apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            var model = GetString("model");
                            return result.SetSuccessResult(new AnthropicClaudeStreamingLLMService(apiKey, model));
                        }

                    case InterfaceLLMProviderEnum.OpenAIGPT:
                        {
                            var apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            var model = GetString("model");
                            var endpoint = "https://api.openai.com/v1";

                            // Check if endpoint override exists in integration config
                            if (integrationData.Fields.TryGetValue("endpoint", out var endpointValue) &&
                                !string.IsNullOrEmpty(endpointValue) &&
                                Uri.IsWellFormedUriString(endpointValue, UriKind.Absolute))
                            {
                                endpoint = endpointValue;
                            }

                            return result.SetSuccessResult(new OpenAIGPTStreamingLLMService(apiKey, model, endpoint));
                        }

                    case InterfaceLLMProviderEnum.GoogleAIGemini:
                        {
                            var apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            var model = GetString("model");
                            return result.SetSuccessResult(new GoogleAIGeminiStreamingLLMService(apiKey, model));
                        }

                    case InterfaceLLMProviderEnum.GroqCloud:
                        {
                            var apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            var model = GetString("model");
                            var logger = loggerFactory.CreateLogger<GroqCloudStreamingLLMService>();
                            return result.SetSuccessResult(new GroqCloudStreamingLLMService(logger, apiKey, model));
                        }

                    default:
                        {
                            _logger.LogError("Business app LLM provider {ProviderType} not supported", llmProviderData.Data.Id);
                            return result.SetFailureResult(
                                "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                                $"Business app LLM provider {llmProviderData.Data.Id} not supported"
                            );
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build provider service");
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    $"Failed to build provider service: {ex.Message}"
                );
            }
        }
    }
}
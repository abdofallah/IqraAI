using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Rerank;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Provider;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Rerank.Providers;
using IqraInfrastructure.Repositories.Rerank;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Rerank
{
    public class RerankProviderManager
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<RerankProviderManager> _logger;

        private readonly RerankProviderRepository _rerankProviderRepository;
        private readonly IntegrationsManager _integrationsManager;

        private readonly Dictionary<InterfaceRerankProviderEnum, Type> _rerankProviderClasses = new();

        public RerankProviderManager(
            ILoggerFactory loggerFactory,
            RerankProviderRepository rerankProviderRepository,
            IntegrationsManager integrationsManager)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<RerankProviderManager>();

            _rerankProviderRepository = rerankProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeProvidersAsync()
        {
            foreach (InterfaceRerankProviderEnum providerEnum in Enum.GetValues(typeof(InterfaceRerankProviderEnum)))
            {
                if (providerEnum == InterfaceRerankProviderEnum.Unknown)
                    continue;

                var provider = await _rerankProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    var addResult = await AddProvider(providerEnum);
                    if (!addResult.Success)
                    {
                        throw new Exception($"Failed to add rerank provider: {providerEnum}: [{addResult.Code}] {addResult.Message}");
                    }
                }

                RegisterProviderService(providerEnum);
            }
        }

        private void RegisterProviderService(InterfaceRerankProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var aiServiceType = typeof(IRerankService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => aiServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderTypeStatic", BindingFlags.Static | BindingFlags.Public);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceRerankProviderEnum)getProviderTypeMethod.Invoke(null, null)!;
                    if (returnedProviderEnum == providerEnum)
                    {
                        _rerankProviderClasses[providerEnum] = type;
                        return;
                    }
                }
            }

            // Warning only as service implementation might not exist yet
            _logger.LogWarning("No matching IRerankService implementation found for provider: {Provider}", providerEnum);
        }

        public async Task<FunctionReturnResult<List<RerankProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<RerankProviderData>?>();

            try
            {
                var providerList = await _rerankProviderRepository.GetProviderListAsync(page, pageSize);
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

        public async Task<FunctionReturnResult<RerankProviderData>> AddProvider(InterfaceRerankProviderEnum providerId)
        {
            var result = new FunctionReturnResult<RerankProviderData>();

            try
            {
                var providerData = new RerankProviderData()
                {
                    Id = providerId,
                    DisabledAt = DateTime.UtcNow
                };

                if (providerData.Id == InterfaceRerankProviderEnum.Unknown)
                {
                    return result.SetFailureResult(
                        "AddProvider:INVALID_ID",
                        "Invalid provider ID"
                    );
                }

                var existingProvider = await _rerankProviderRepository.GetProviderAsync(providerData.Id);
                if (existingProvider != null)
                {
                    return result.SetFailureResult(
                        "AddProvider:EXISTS",
                        "Provider already exists"
                    );
                }

                var success = await _rerankProviderRepository.AddProviderAsync(providerData);
                if (!success)
                {
                    return result.SetFailureResult(
                        "AddProvider:FAILED",
                        "Failed to add provider"
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

        public Type? GetProviderService(InterfaceRerankProviderEnum providerId)
        {
            return _rerankProviderClasses.TryGetValue(providerId, out var service) ? service : null;
        }

        public async Task<RerankProviderData?> GetProviderData(InterfaceRerankProviderEnum providerId)
        {
            return await _rerankProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<RerankProviderModelData?>> AddUpdateProviderModel(
            RerankProviderData provider,
            string modelId,
            string postType,
            RerankProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderModelData?>();

            try
            {
                var newModelData = new RerankProviderModelData { Id = modelId };

                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:CHANGES_DATA_NOT_FOUND",
                        "Changes data not found or is empty"
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
                if (root.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                {
                    string? name = nameElement.GetString();
                    if (string.IsNullOrEmpty(name))
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:EMPTY_NAME",
                            "Model name is empty"
                        );
                    }
                    newModelData.Name = name;
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:NAME_NOT_FOUND",
                        "Model name is required"
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

                // Price
                if (root.TryGetProperty("price", out var priceElement))
                {
                    if (decimal.TryParse(priceElement.ToString(), out var price))
                    {
                        newModelData.Price = price;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_PRICE",
                            "Invalid price format"
                        );
                    }
                }

                // Price Token Unit
                if (root.TryGetProperty("priceTokenUnit", out var priceTokenUnitElement))
                {
                    if (int.TryParse(priceTokenUnitElement.ToString(), out var priceTokenUnit))
                    {
                        newModelData.PriceTokenUnit = priceTokenUnit;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_PRICE_TOKEN_UNIT",
                            "Invalid price token unit format"
                        );
                    }
                }

                // Save to database
                bool updateSuccess;
                if (postType == "new")
                {
                    var addResult = await _rerankProviderRepository.AddModelAsync(provider.Id, newModelData);
                    updateSuccess = addResult.IsAcknowledged && addResult.ModifiedCount > 0;
                }
                else
                {
                    var updateResult = await _rerankProviderRepository.UpdateModelAsync(provider.Id, newModelData);
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

        public async Task<FunctionReturnResult<RerankProviderData?>> UpdateProvider(
            RerankProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

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
                var newProviderData = new RerankProviderData
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

                    // Validate integration exists and is Rerank type
                    var integration = await integrationsManager.getIntegrationData(integrationId);
                    if (integration.Data == null || !integration.Success)
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:SELECTED_INTEGRATION_NOT_FOUND",
                            "Selected integration not found"
                        );
                    }

                    if (!integration.Data.Type.Contains("Rerank"))
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:INVALID_INTEGRATION",
                            "Selected integration is not an Rerank integration"
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
                var updateResult = await _rerankProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<RerankProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

            try
            {
                var providerData = await _rerankProviderRepository.GetProviderDataByIntegration(integrationType);

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

        public async Task<FunctionReturnResult<IRerankService?>> BuildProviderServiceByIntegration(
            BusinessAppIntegration integrationData,
            BusinessAppAgentIntegrationData agentIntegrationData,
            Dictionary<string, string> metaData)
        {
            var result = new FunctionReturnResult<IRerankService?>();

            try
            {
                var providerDataResult = await _rerankProviderRepository.GetProviderDataByIntegration(integrationData.Type);
                if (providerDataResult == null)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:PROVIDER_NOT_FOUND",
                        $"Rerank provider not found for integration type {integrationData.Type}"
                    );
                }

                // --- Helper functions for safe extraction ---
                string GetString(string key, string defaultValue = "")
                {
                    return agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null
                        ? val.ToString()! : defaultValue;
                }
                // ---------------------------------------------

                switch (providerDataResult.Id)
                {
                    case InterfaceRerankProviderEnum.GoogleGemini:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string model = GetString("model");

                            var logger = _loggerFactory.CreateLogger<GoogleGeminiRerankService>();
                            return result.SetSuccessResult(new GoogleGeminiRerankService(logger, apiKey, model));
                        }

                    default:
                        {
                            _logger.LogError("Business app Rerank provider {ProviderType} not supported for building service", providerDataResult.Id);
                            return result.SetFailureResult(
                                "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                                $"Business app Rerank provider {providerDataResult.Id} not supported"
                            );
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build rerank provider service");
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    $"Failed to build provider service: {ex.Message}"
                );
            }
        }
    }
}
using IqraCore.Entities.Business;
using IqraCore.Entities.Embedding;
using IqraCore.Entities.Embedding.Providers.GoogleGemini;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Provider;
using IqraInfrastructure.Managers.Embedding.Providers;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Repositories.Embedding;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Embedding
{
    public class EmbeddingProviderManager
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<EmbeddingProviderManager> _logger;

        private readonly EmbeddingProviderRepository _embeddingProviderRepository;
        private readonly IntegrationsManager _integrationsManager;

        private readonly Dictionary<InterfaceEmbeddingProviderEnum, Type> _embeddingProviderClasses = new();

        public EmbeddingProviderManager(
            ILoggerFactory loggerFactory,
            EmbeddingProviderRepository embeddingProviderRepository,
            IntegrationsManager integrationsManager)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EmbeddingProviderManager>();

            _embeddingProviderRepository = embeddingProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeProvidersAsync()
        {
            foreach (InterfaceEmbeddingProviderEnum providerEnum in Enum.GetValues(typeof(InterfaceEmbeddingProviderEnum)))
            {
                if (providerEnum == InterfaceEmbeddingProviderEnum.Unknown)
                    continue;

                var provider = await _embeddingProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    var addResult = await AddProvider(providerEnum);
                    if (!addResult.Success)
                    {
                        throw new Exception($"Failed to add embedding provider: {providerEnum}: [{addResult.Code}] {addResult.Message}");
                    }
                }

                RegisterProviderService(providerEnum);
            }
        }

        private void RegisterProviderService(InterfaceEmbeddingProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var aiServiceType = typeof(IEmbeddingService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => aiServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderTypeStatic", BindingFlags.Static | BindingFlags.Public);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceEmbeddingProviderEnum)getProviderTypeMethod.Invoke(null, null)!;
                    if (returnedProviderEnum == providerEnum)
                    {
                        _embeddingProviderClasses[providerEnum] = type;
                        return;
                    }
                }
            }

            // Warning only as service implementation might not exist yet
            _logger.LogWarning("No matching IEmbeddingService implementation found for provider: {Provider}", providerEnum);
        }

        public async Task<FunctionReturnResult<List<EmbeddingProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<EmbeddingProviderData>?>();

            try
            {
                var providerList = await _embeddingProviderRepository.GetProviderListAsync(page, pageSize);
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

        public async Task<FunctionReturnResult<EmbeddingProviderData>> AddProvider(InterfaceEmbeddingProviderEnum providerId)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData>();

            try
            {
                var providerData = new EmbeddingProviderData()
                {
                    Id = providerId,
                    DisabledAt = DateTime.UtcNow
                };

                if (providerData.Id == InterfaceEmbeddingProviderEnum.Unknown)
                {
                    return result.SetFailureResult(
                        "AddProvider:INVALID_ID",
                        "Invalid provider ID"
                    );
                }

                var existingProvider = await _embeddingProviderRepository.GetProviderAsync(providerData.Id);
                if (existingProvider != null)
                {
                    return result.SetFailureResult(
                        "AddProvider:EXISTS",
                        "Provider already exists"
                    );
                }

                var success = await _embeddingProviderRepository.AddProviderAsync(providerData);
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

        public Type? GetProviderService(InterfaceEmbeddingProviderEnum providerId)
        {
            return _embeddingProviderClasses.TryGetValue(providerId, out var service) ? service : null;
        }

        public async Task<EmbeddingProviderData?> GetProviderData(InterfaceEmbeddingProviderEnum providerId)
        {
            return await _embeddingProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<EmbeddingProviderModelData?>> AddUpdateProviderModel(
            EmbeddingProviderData provider,
            string modelId,
            string postType,
            EmbeddingProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderModelData?>();

            try
            {
                var newModelData = new EmbeddingProviderModelData { Id = modelId };

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

                // Available Vector Dimensions
                if (root.TryGetProperty("availableVectorDimensions", out var dimensionsElement) && dimensionsElement.ValueKind == JsonValueKind.Array)
                {
                    newModelData.AvailableVectorDimensions = new List<int>();
                    foreach (var dimElement in dimensionsElement.EnumerateArray())
                    {
                        if (dimElement.TryGetInt32(out int dim))
                        {
                            newModelData.AvailableVectorDimensions.Add(dim);
                        }
                    }
                }

                // Save to database
                bool updateSuccess;
                if (postType == "new")
                {
                    var addResult = await _embeddingProviderRepository.AddModelAsync(provider.Id, newModelData);
                    updateSuccess = addResult.IsAcknowledged && addResult.ModifiedCount > 0;
                }
                else
                {
                    var updateResult = await _embeddingProviderRepository.UpdateModelAsync(provider.Id, newModelData);
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

        public async Task<FunctionReturnResult<EmbeddingProviderData?>> UpdateProvider(
            EmbeddingProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

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
                var newProviderData = new EmbeddingProviderData
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

                    // Validate integration exists and is Embedding type
                    var integration = await integrationsManager.getIntegrationData(integrationId);
                    if (integration.Data == null || !integration.Success)
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:SELECTED_INTEGRATION_NOT_FOUND",
                            "Selected integration not found"
                        );
                    }

                    if (!integration.Data.Type.Contains("Embedding"))
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:INVALID_INTEGRATION",
                            "Selected integration is not an Embedding integration"
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
                var updateResult = await _embeddingProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<EmbeddingProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            try
            {
                var providerData = await _embeddingProviderRepository.GetProviderDataByIntegration(integrationType);

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

        public async Task<FunctionReturnResult<IEmbeddingService?>> BuildProviderServiceByIntegration(
            BusinessAppIntegration integrationData,
            BusinessAppAgentIntegrationData agentIntegrationData)
        {
            var result = new FunctionReturnResult<IEmbeddingService?>();

            try
            {
                var providerDataResult = await _embeddingProviderRepository.GetProviderDataByIntegration(integrationData.Type);
                if (providerDataResult == null)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:PROVIDER_NOT_FOUND",
                        $"Embedding provider not found for integration type {integrationData.Type}"
                    );
                }

                // --- Helper functions for safe extraction ---
                string GetString(string key, string defaultValue = "")
                {
                    return agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null
                        ? val.ToString()! : defaultValue;
                }

                int GetInt(string key, int defaultValue)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        if (int.TryParse(val.ToString(), out int parsed)) return parsed;
                        return Convert.ToInt32(val);
                    }
                    return defaultValue;
                }
                // ---------------------------------------------

                switch (providerDataResult.Id)
                {
                    case InterfaceEmbeddingProviderEnum.GoogleGemini:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string model = GetString("model");
                            int vectorDimension = GetInt("model_vector_dimension", 0);

                            var config = new GoogleGeminiEmbeddingServiceConfig()
                            {
                                Model = model,
                                VectorDimension = vectorDimension
                            };

                            var service = new GoogleGeminiEmbeddingService(_loggerFactory.CreateLogger<GoogleGeminiEmbeddingService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    default:
                        {
                            _logger.LogError("Business app Embedding provider {ProviderType} not supported for building service", providerDataResult.Id);
                            return result.SetFailureResult(
                                "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                                $"Business app Embedding provider {providerDataResult.Id} not supported"
                            );
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build embedding provider service");
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    $"Failed to build provider service: {ex.Message}"
                );
            }
        }
    }
}
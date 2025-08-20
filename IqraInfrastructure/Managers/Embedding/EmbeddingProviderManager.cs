using IqraCore.Entities.Business;
using IqraCore.Entities.Embedding;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Interfaces.AI;
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

        public EmbeddingProviderManager(ILoggerFactory loggerFactory, EmbeddingProviderRepository embeddingProviderRepository, IntegrationsManager integrationsManager)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EmbeddingProviderManager>();

            _embeddingProviderRepository = embeddingProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().Wait();
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
                    provider = new EmbeddingProviderData
                    {
                        Id = providerEnum,
                        DisabledAt = DateTime.UtcNow
                    };
                    await AddProvider(provider);
                }

                if (provider.DisabledAt == null)
                {
                    RegisterProviderService(providerEnum);
                }
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

            // This is not a critical error if a service implementation is not yet created.
            _logger.LogWarning("No matching IEmbeddingService implementation found for provider: {Provider}", providerEnum);
        }

        public async Task<FunctionReturnResult<List<EmbeddingProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<EmbeddingProviderData>?>();
            var providerList = await _embeddingProviderRepository.GetProviderListAsync(page, pageSize);
            if (providerList == null)
            {
                return result.SetFailureResult("GetProviderList:1", "No providers found");
            }

            result.Success = true;
            result.Data = providerList;
            return result;
        }

        public async Task<FunctionReturnResult<EmbeddingProviderData>> AddProvider(EmbeddingProviderData providerData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData>();

            if (providerData.Id == InterfaceEmbeddingProviderEnum.Unknown)
            {
                return result.SetFailureResult("AddProvider:1", "Invalid provider ID");
            }

            var existingProvider = await _embeddingProviderRepository.GetProviderAsync(providerData.Id);
            if (existingProvider != null)
            {
                return result.SetFailureResult("AddProvider:2", "Provider already exists");
            }

            providerData.DisabledAt = DateTime.UtcNow;
            await _embeddingProviderRepository.AddProviderAsync(providerData);

            return result.SetSuccessResult(providerData);
        }

        public async Task<FunctionReturnResult> DisableProvider(InterfaceEmbeddingProviderEnum providerId)
        {
            var result = new FunctionReturnResult();
            if (providerId == InterfaceEmbeddingProviderEnum.Unknown)
            {
                return result.SetFailureResult("DisableProvider:1", "Invalid provider ID");
            }

            var updateResult = await _embeddingProviderRepository.DisableProviderAsync(providerId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                return result.SetFailureResult("DisableProvider:2", "Provider not found or already disabled");
            }

            _embeddingProviderClasses.Remove(providerId);
            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> EnableProvider(InterfaceEmbeddingProviderEnum providerId)
        {
            var result = new FunctionReturnResult();
            if (providerId == InterfaceEmbeddingProviderEnum.Unknown)
            {
                return result.SetFailureResult("EnableProvider:1", "Invalid provider ID");
            }

            var updateResult = await _embeddingProviderRepository.EnableProviderAsync(providerId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                return result.SetFailureResult("EnableProvider:2", "Provider not found or already enabled");
            }

            try
            {
                RegisterProviderService(providerId);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("EnableProvider:3", $"Failed to register provider service: {ex.Message}");
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> AddModel(InterfaceEmbeddingProviderEnum providerId, EmbeddingProviderModelData modelData)
        {
            var result = new FunctionReturnResult();
            if (providerId == InterfaceEmbeddingProviderEnum.Unknown)
            {
                return result.SetFailureResult("AddModel:1", "Invalid provider ID");
            }

            var updateResult = await _embeddingProviderRepository.AddModelAsync(providerId, modelData);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                return result.SetFailureResult("AddModel:2", "Provider not found or model already exists");
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> DisableModel(InterfaceEmbeddingProviderEnum providerId, string modelId)
        {
            var result = new FunctionReturnResult();
            if (providerId == InterfaceEmbeddingProviderEnum.Unknown)
            {
                return result.SetFailureResult("DisableModel:1", "Invalid provider ID");
            }

            var updateResult = await _embeddingProviderRepository.DisableModelAsync(providerId, modelId);
            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                return result.SetFailureResult("DisableModel:2", "Provider or model not found");
            }

            return result.SetSuccessResult();
        }

        public Type? GetProviderService(InterfaceEmbeddingProviderEnum providerId)
        {
            _embeddingProviderClasses.TryGetValue(providerId, out var service);
            return service;
        }

        public async Task<EmbeddingProviderData?> GetProviderData(InterfaceEmbeddingProviderEnum providerId)
        {
            return await _embeddingProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<EmbeddingProviderModelData?>> AddUpdateProviderModel(EmbeddingProviderData provider, string modelId, string postType, EmbeddingProviderModelData? oldModelData, IFormCollection formData)
        {
            var result = new FunctionReturnResult<EmbeddingProviderModelData?>();

            var newModelData = new EmbeddingProviderModelData { Id = modelId };

            if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
            {
                return result.SetFailureResult("AddUpdateProviderModel:1", "Changes data not found or is empty");
            }

            JsonDocument changesJson;
            try
            {
                changesJson = JsonDocument.Parse(changesJsonString);
            }
            catch (JsonException ex)
            {
                return result.SetFailureResult("AddUpdateProviderModel:2", $"Invalid JSON format for changes: {ex.Message}");
            }

            var root = changesJson.RootElement;

            // Model Name
            if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(nameElement.GetString()))
            {
                return result.SetFailureResult("AddUpdateProviderModel:3", "Model name is required");
            }
            newModelData.Name = nameElement.GetString()!;

            // Disabled
            if (root.TryGetProperty("disabled", out var disabledElement) && disabledElement.GetBoolean())
            {
                newModelData.DisabledAt = (postType == "edit" && oldModelData?.DisabledAt != null) ? oldModelData.DisabledAt : DateTime.UtcNow;
            }
            else
            {
                newModelData.DisabledAt = null;
            }

            // Price
            if (root.TryGetProperty("price", out var priceElement) && decimal.TryParse(priceElement.ToString(), out var price))
            {
                newModelData.Price = price;
            }

            // Price Token Unit
            if (root.TryGetProperty("priceTokenUnit", out var priceTokenUnitElement) && int.TryParse(priceTokenUnitElement.ToString(), out var priceTokenUnit))
            {
                newModelData.PriceTokenUnit = priceTokenUnit;
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

            // Saving new data to database
            if (postType == "new")
            {
                var addResult = await _embeddingProviderRepository.AddModelAsync(provider.Id, newModelData);
                if (!addResult.IsAcknowledged || addResult.ModifiedCount == 0)
                {
                    return result.SetFailureResult("AddUpdateProviderModel:4", "Failed to add model");
                }
            }
            else if (postType == "edit")
            {
                var editResult = await _embeddingProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                if (!editResult.IsAcknowledged || editResult.ModifiedCount == 0)
                {
                    return result.SetFailureResult("AddUpdateProviderModel:5", "Failed to edit model or no changes detected");
                }
            }

            return result.SetSuccessResult(newModelData);
        }

        public async Task<FunctionReturnResult<EmbeddingProviderData?>> UpdateProvider(EmbeddingProviderData provider, IFormCollection formData, IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    result.Code = "UpdateProvider:1";
                    result.Message = "Changes data not found";
                    return result;
                }

                var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString);
                if (changesJsonElement == null)
                {
                    result.Code = "UpdateProvider:2";
                    result.Message = "Unable to parse changes json string.";
                    return result;
                }

                var newProviderData = new EmbeddingProviderData
                {
                    Id = provider.Id,
                    Models = provider.Models // Maintain existing models
                };

                // Handle disabled state
                if (!changesJsonElement.RootElement.TryGetProperty("disabled", out var disabledElement))
                {
                    result.Code = "UpdateProvider:3";
                    result.Message = "Provider disabled state not found";
                    return result;
                }

                bool disabled = disabledElement.GetBoolean();
                if (disabled)
                {
                    if (provider.DisabledAt != null)
                    {
                        newProviderData.DisabledAt = provider.DisabledAt;
                    }
                    else
                    {
                        newProviderData.DisabledAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    newProviderData.DisabledAt = null;
                }

                // Handle integration selection
                if (!changesJsonElement.RootElement.TryGetProperty("integrationId", out var integrationIdElement))
                {
                    result.Code = "UpdateProvider:4";
                    result.Message = "Integration ID not found";
                    return result;
                }

                string? integrationId = integrationIdElement.GetString();
                if (string.IsNullOrEmpty(integrationId))
                {
                    result.Code = "UpdateProvider:5";
                    result.Message = "Integration ID is required";
                    return result;
                }

                // Validate that integration exists
                var integration = await integrationsManager.getIntegrationData(integrationId);
                if (integration == null || !integration.Success)
                {
                    result.Code = "UpdateProvider:6";
                    result.Message = "Selected integration not found";
                    return result;
                }

                // Validate integration type includes LLM
                if (!integration.Data.Type.Contains("Embedding"))
                {
                    result.Code = "UpdateProvider:7";
                    result.Message = "Selected integration is not an Embedding integration";
                    return result;
                }

                newProviderData.IntegrationId = integrationId;

                // Handle integration fields
                if (changesJsonElement.RootElement.TryGetProperty("userIntegrationFields", out var fieldsElement))
                {
                    newProviderData.UserIntegrationFields = new List<ProviderFieldBase>();

                    foreach (var fieldElement in fieldsElement.EnumerateArray())
                    {
                        var field = new ProviderFieldBase
                        {
                            Id = fieldElement.GetProperty("id").GetString() ?? "",
                            Name = fieldElement.GetProperty("name").GetString() ?? "",
                            Type = fieldElement.GetProperty("type").GetString() ?? "",
                            Tooltip = fieldElement.GetProperty("tooltip").GetString() ?? "",
                            Placeholder = fieldElement.GetProperty("placeholder").GetString() ?? "",
                            DefaultValue = fieldElement.GetProperty("defaultValue").GetString() ?? "",
                            Required = fieldElement.GetProperty("required").GetBoolean(),
                            IsEncrypted = fieldElement.GetProperty("isEncrypted").GetBoolean()
                        };

                        // Handle options for select type
                        if (field.Type == "select" && fieldElement.TryGetProperty("options", out var optionsElement))
                        {
                            field.Options = new List<ProviderFieldOption>();
                            foreach (var optionElement in optionsElement.EnumerateArray())
                            {
                                field.Options.Add(new ProviderFieldOption
                                {
                                    Key = optionElement.GetProperty("key").GetString() ?? "",
                                    Value = optionElement.GetProperty("value").GetString() ?? "",
                                    IsDefault = optionElement.GetProperty("isDefault").GetBoolean()
                                });
                            }
                        }

                        newProviderData.UserIntegrationFields.Add(field);
                    }
                }

                // Save to database
                var updateResult = await _embeddingProviderRepository.UpdateProviderAsync(newProviderData);
                if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
                {
                    result.Code = "UpdateProvider:8";
                    result.Message = "Failed to update provider";
                    return result;
                }

                result.Success = true;
                result.Data = newProviderData;
            }
            catch (Exception ex)
            {
                result.Code = "UpdateProvider:9";
                result.Message = "Error processing provider update: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<EmbeddingProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<EmbeddingProviderData?>();

            try
            {
                var providerData = await _embeddingProviderRepository.GetProviderDataByIntegration(integrationType);

                if (providerData == null)
                {
                    result.Code = "GetProviderDataByIntegration:1";
                    result.Message = "Provider not find by integration type";
                    return result;
                }

                result.Success = true;
                result.Data = providerData;
            }
            catch (Exception ex)
            {
                result.Code = "GetProviderDataByIntegration:2";
                result.Message = "Failed to get provider data: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<IEmbeddingService?>> BuildProviderServiceByIntegration(BusinessAppIntegration integrationData, BusinessAppAgentIntegrationData agentIntegrationData)
        {
            var result = new FunctionReturnResult<IEmbeddingService?>();
            try
            {
                var providerDataResult = await _embeddingProviderRepository.GetProviderDataByIntegration(integrationData.Type);
                if (providerDataResult == null)
                {
                    return result.SetFailureResult("BuildProviderService:1", $"Embedding provider not found for integration type {integrationData.Type}");
                }

                string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                string model = (string)agentIntegrationData.FieldValues["model"];

                switch (providerDataResult.Id)
                {
                    case InterfaceEmbeddingProviderEnum.GoogleGemini:
                        {
                            var service = new GoogleGeminiEmbeddingService(_loggerFactory.CreateLogger<GoogleGeminiEmbeddingService>(), apiKey, model, (int)agentIntegrationData.FieldValues["model_vector_dimension"]);
                            return result.SetSuccessResult(service);
                        }

                    default:
                        _logger.LogError("Business app Embedding provider {ProviderType} not supported for building service", providerDataResult.Id);
                        return result.SetFailureResult("BuildProviderService:2", $"Business app Embedding provider {providerDataResult.Id} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build embedding provider service");
                return result.SetFailureResult("BuildProviderService:EXCEPTION", $"Failed to build provider service: {ex.Message}");
            }
        }
    }
}

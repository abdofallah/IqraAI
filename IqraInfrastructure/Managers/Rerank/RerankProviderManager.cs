using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.Rerank;
using IqraCore.Interfaces.AI;
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

        public RerankProviderManager(ILoggerFactory loggerFactory, RerankProviderRepository rerankProviderRepository, IntegrationsManager integrationsManager)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<RerankProviderManager>();

            _rerankProviderRepository = rerankProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().Wait();
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
                    provider = new RerankProviderData
                    {
                        Id = providerEnum,
                        DisabledAt = DateTime.UtcNow
                    };
                    await _rerankProviderRepository.AddProviderAsync(provider);
                }

                if (provider.DisabledAt == null)
                {
                    RegisterProviderService(providerEnum);
                }
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
            _logger.LogWarning("No matching IRerankService implementation found for provider: {Provider}", providerEnum);
        }

        public async Task<RerankProviderData?> GetProviderData(InterfaceRerankProviderEnum providerId)
        {
            return await _rerankProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<List<RerankProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<RerankProviderData>?>();
            var providerList = await _rerankProviderRepository.GetProviderListAsync(page, pageSize);
            if (providerList == null)
            {
                return result.SetFailureResult("GetProviderList:1", "No providers found");
            }

            result.Success = true;
            result.Data = providerList;
            return result;
        }

        public async Task<FunctionReturnResult<RerankProviderData?>> UpdateProvider(RerankProviderData provider, IFormCollection formData, IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

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

                var newProviderData = new RerankProviderData
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
                if (!integration.Data.Type.Contains("Rerank"))
                {
                    result.Code = "UpdateProvider:7";
                    result.Message = "Selected integration is not an Rerank integration";
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
                var updateResult = await _rerankProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<RerankProviderModelData?>> AddUpdateProviderModel(RerankProviderData provider, string modelId, string postType, RerankProviderModelData? oldModelData, IFormCollection formData)
        {
            var result = new FunctionReturnResult<RerankProviderModelData?>();

            var newModelData = new RerankProviderModelData { Id = modelId };

            if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
            {
                return result.SetFailureResult("AddUpdateProviderModel:1", "Changes data not found or is empty");
            }

            var root = JsonDocument.Parse(changesJsonString).RootElement;

            if (!root.TryGetProperty("name", out var nameElement) || string.IsNullOrEmpty(nameElement.GetString()))
            {
                return result.SetFailureResult("AddUpdateProviderModel:2", "Model name is required");
            }
            newModelData.Name = nameElement.GetString()!;

            if (root.TryGetProperty("disabled", out var disabledElement) && disabledElement.GetBoolean())
            {
                newModelData.DisabledAt = (postType == "edit" && oldModelData?.DisabledAt != null) ? oldModelData.DisabledAt : DateTime.UtcNow;
            }

            if (root.TryGetProperty("price", out var priceElement) && decimal.TryParse(priceElement.ToString(), out var price))
            {
                newModelData.Price = price;
            }
            if (root.TryGetProperty("priceTokenUnit", out var unitElement) && int.TryParse(unitElement.ToString(), out var unit))
            {
                newModelData.PriceTokenUnit = unit;
            }

            if (postType == "new")
            {
                var addResult = await _rerankProviderRepository.AddModelAsync(provider.Id, newModelData);
                if (!addResult.IsAcknowledged || addResult.ModifiedCount == 0)
                {
                    return result.SetFailureResult("AddUpdateProviderModel:3", "Failed to add model");
                }
            }
            else if (postType == "edit")
            {
                var editResult = await _rerankProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                if (!editResult.IsAcknowledged || editResult.ModifiedCount == 0)
                {
                    return result.SetFailureResult("AddUpdateProviderModel:4", "Failed to edit model or no changes detected");
                }
            }

            return result.SetSuccessResult(newModelData);
        }

        public async Task<FunctionReturnResult<RerankProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<RerankProviderData?>();

            try
            {
                var providerData = await _rerankProviderRepository.GetProviderDataByIntegration(integrationType);

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

        public async Task<FunctionReturnResult<IRerankService?>> BuildProviderServiceByIntegration(BusinessAppIntegration integrationData, BusinessAppAgentIntegrationData agentIntegrationData, Dictionary<string, string> metaData)
        {
            var result = new FunctionReturnResult<IRerankService?>();
            try
            {
                var providerDataResult = await _rerankProviderRepository.GetProviderDataByIntegration(integrationData.Type);
                if (providerDataResult == null)
                {
                    return result.SetFailureResult("BuildProviderService:1", $"Rerank provider not found for integration type {integrationData.Type}");
                }

                string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                string model = (string)agentIntegrationData.FieldValues["model"];

                switch (providerDataResult.Id)
                {
                    case InterfaceRerankProviderEnum.GoogleGemini:
                        var logger = _loggerFactory.CreateLogger<GoogleGeminiRerankService>();
                        return result.SetSuccessResult(new GoogleGeminiRerankService(logger, apiKey, model));

                    default:
                        _logger.LogError("Business app Rerank provider {ProviderType} not supported for building service", providerDataResult.Id);
                        return result.SetFailureResult("BuildProviderService:2", $"Business app Rerank provider {providerDataResult.Id} not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build rerank provider service");
                return result.SetFailureResult("BuildProviderService:EXCEPTION", $"Failed to build provider service: {ex.Message}");
            }
        }
    }
}

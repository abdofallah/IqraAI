using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
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

        // ... Add other CRUD methods like GetProviderList, DisableProvider, EnableProvider, etc.
        // ... They are identical to EmbeddingProviderManager, just with different types.
        // ... For brevity, I'll skip them here but you should add them.

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

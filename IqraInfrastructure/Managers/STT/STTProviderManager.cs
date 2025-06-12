using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.STT;
using IqraInfrastructure.Repositories.STT;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json;
using IqraCore.Interfaces.AI;
using IqraCore.Entities.Business;
using IqraInfrastructure.Managers.STT.Providers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IqraInfrastructure.Managers.STT
{
    public class STTProviderManager
    {
        private readonly ILogger<STTProviderManager> _logger;

        private readonly STTProviderRepository _sttProviderRepository;
        private readonly IntegrationsManager _integrationsManager;

        private Dictionary<InterfaceSTTProviderEnum, Type> _sttProviderClasses = new Dictionary<InterfaceSTTProviderEnum, Type>();

        public STTProviderManager(ILogger<STTProviderManager> logger, STTProviderRepository sttProviderRepository, IntegrationsManager integrationsManager)
        {
            _logger = logger;

            _sttProviderRepository = sttProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().Wait();
        }

        private async Task InitializeProvidersAsync()
        {
            foreach (InterfaceSTTProviderEnum providerEnum in Enum.GetValues(typeof(InterfaceSTTProviderEnum)))
            {
                if (providerEnum == InterfaceSTTProviderEnum.Unknown)
                    continue;

                var provider = await _sttProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    provider = new STTProviderData
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

        private void RegisterProviderService(InterfaceSTTProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var sttServiceType = typeof(ISTTService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => sttServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderTypeStatic", BindingFlags.Static | BindingFlags.Public);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceSTTProviderEnum)getProviderTypeMethod.Invoke(null, null);
                    if (returnedProviderEnum == providerEnum)
                    {
                        _sttProviderClasses[providerEnum] = type;
                        return;
                    }
                }
            }

            throw new Exception($"No matching ISTTService implementation found for provider: {providerEnum}");
        }

        public async Task<FunctionReturnResult<List<STTProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<STTProviderData>?>();

            var providerList = await _sttProviderRepository.GetProviderListAsync(page, pageSize);
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

        public async Task<FunctionReturnResult<STTProviderData>> AddProvider(STTProviderData providerData)
        {
            var result = new FunctionReturnResult<STTProviderData>();

            if (providerData.Id == InterfaceSTTProviderEnum.Unknown)
            {
                result.Code = "AddProvider:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var existingProvider = await _sttProviderRepository.GetProviderAsync(providerData.Id);
            if (existingProvider != null)
            {
                result.Code = "AddProvider:2";
                result.Message = "Provider already exists";
                return result;
            }

            providerData.DisabledAt = DateTime.UtcNow;
            await _sttProviderRepository.AddProviderAsync(providerData);

            result.Success = true;
            result.Data = providerData;
            return result;
        }

        public Type? GetProviderService(InterfaceSTTProviderEnum providerId)
        {
            if (_sttProviderClasses.TryGetValue(providerId, out var service))
            {
                return service;
            }
            return null;
        }

        public async Task<STTProviderData?> GetProviderData(InterfaceSTTProviderEnum providerId)
        {
            return await _sttProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<STTProviderModelData?>> AddUpdateProviderModel(
            STTProviderData provider,
            string modelId,
            string postType,
            STTProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderModelData?>();

            var newModelData = new STTProviderModelData()
            {
                Id = modelId
            };

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddUpdateProviderModel:1";
                result.Message = "Changes data not found";
                return result;
            }

            JsonDocument changesJsonElement;
            try
            {
                changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString);
            }
            catch (JsonException)
            {
                result.Code = "AddUpdateProviderModel:2";
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

            // Price Per Unit
            if (changesJsonElement.RootElement.TryGetProperty("pricePerUnit", out var priceElement))
            {
                if (decimal.TryParse(priceElement.GetString(), out decimal price))
                {
                    newModelData.PricePerUnit = price;
                }
                else
                {
                    result.Code = "AddUpdateProviderModel:6";
                    result.Message = "Invalid price";
                    return result;
                }
            }

            // Price Unit
            if (changesJsonElement.RootElement.TryGetProperty("priceUnit", out var priceUnitElement))
            {
                string? priceUnit = priceUnitElement.GetString();
                if (string.IsNullOrEmpty(priceUnit))
                {
                    result.Code = "AddUpdateProviderModel:7";
                    result.Message = "Price unit is required";
                    return result;
                }
                newModelData.PriceUnit = priceUnit;
            }

            // Supported Languages
            if (changesJsonElement.RootElement.TryGetProperty("supportedLanguages", out var languagesElement))
            {
                newModelData.SupportedLanguages = new List<string>();
                foreach (var language in languagesElement.EnumerateArray())
                {
                    newModelData.SupportedLanguages.Add(language.GetString() ?? "");
                }
            }

            // Saving new data to database
            if (postType == "new")
            {
                var addResult = await _sttProviderRepository.AddModelAsync(provider.Id, newModelData);
                if (!addResult.IsAcknowledged || addResult.ModifiedCount == 0)
                {
                    result.Code = "AddUpdateProviderModel:8";
                    result.Message = "Failed to add model";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                var editResult = await _sttProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                if (!editResult.IsAcknowledged || editResult.ModifiedCount == 0)
                {
                    result.Code = "AddUpdateProviderModel:9";
                    result.Message = "Failed to edit model";
                    return result;
                }
            }

            result.Data = newModelData;
            result.Success = true;
            return result;
        }

        public async Task<FunctionReturnResult<STTProviderData?>> UpdateProvider(
            STTProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

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
                    result.Message = "Unable to parse changes json string";
                    return result;
                }

                var newProviderData = new STTProviderData
                {
                    Id = provider.Id,
                    Models = provider.Models
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

                // Validate integration exists and is STT type
                var integration = await integrationsManager.getIntegrationData(integrationId);
                if (integration == null || !integration.Success)
                {
                    result.Code = "UpdateProvider:6";
                    result.Message = "Selected integration not found";
                    return result;
                }

                if (!integration.Data.Type.Contains("STT") && !integration.Data.Type.Contains("SPEECH2TEXT"))
                {
                    result.Code = "UpdateProvider:7";
                    result.Message = "Selected integration is not an STT integration";
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
                var updateResult = await _sttProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<STTProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

            try
            {
                var providerData = await _sttProviderRepository.GetProviderDataByIntegration(integrationType);

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
    
        public async Task<FunctionReturnResult<ISTTService?>> BuildProviderServiceByIntegration(BusinessAppIntegration integrationData, BusinessAppAgentIntegrationData agentIntegrationData, Dictionary<string, string> metaData)
        {
            var result = new FunctionReturnResult<ISTTService?>();

            try
            {
                var sttProviderData = await GetProviderDataByIntegration(integrationData.Type);
                if (!sttProviderData.Success)
                {
                    result.Code = "BuildProviderServiceByIntegration:1";
                    result.Message = "Provider not find by integration type";
                    return result;
                }

                int sampleRate = 8000;

                switch (sttProviderData.Data.Id)
                {
                    case InterfaceSTTProviderEnum.AzureSpeechServices:
                        string resourceKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["resource_key"]);
                        string resourceRegion = integrationData.Fields["resource_region"];
                        string languageId = (string)agentIntegrationData.FieldValues["langauge_id"];
                        string? continousLanguageIdentificationIdsString = (string?)agentIntegrationData.FieldValues["continous_language_identification_ids"];
                        string speakerDiarizationString = (string)agentIntegrationData.FieldValues["speaker_diarization"];
                        string? phrasesListString = (string?)agentIntegrationData.FieldValues["phrases_list"];
                        int silenceTimeout = (int)agentIntegrationData.FieldValues["silence_timeout"];

                        List<string> continousLanguageIdentificationIds = new List<string>();
                        if (!string.IsNullOrEmpty(continousLanguageIdentificationIdsString))
                        {
                            continousLanguageIdentificationIds.AddRange(continousLanguageIdentificationIdsString.Split(','));
                        }

                        bool speakerDiarization = (speakerDiarizationString == "on");

                        List<string> phrasesList = new List<string>();
                        if (!string.IsNullOrEmpty(phrasesListString))
                        {
                            phrasesList.AddRange(phrasesListString.Split(','));
                        }

                        var azureSTTService = new AzureSpeechSTTService(resourceKey, resourceRegion, languageId, continousLanguageIdentificationIds, speakerDiarization, phrasesList, silenceTimeout, sampleRate);
                        return result.SetSuccessResult(
                            azureSTTService
                        );

                    default:
                        _logger.LogError("Business app STT provider {ProviderType} not supported", sttProviderData.Data.Id);
                        return result;
                }
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    ("Failed to build provider service: " + ex.Message)
                );
            }
        }
    }
}
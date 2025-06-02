using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Repositories.TTS;
using IqraInfrastructure.Managers.Integrations;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json;
using IqraCore.Entities.Business;
using IqraInfrastructure.Managers.TTS.Providers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.TTS
{
    public class TTSProviderManager
    {
        private readonly ILogger<TTSProviderManager> _logger;

        private readonly TTSProviderRepository _ttsProviderRepository;
        private readonly IntegrationsManager _integrationsManager;

        private Dictionary<InterfaceTTSProviderEnum, Type> _ttsProviderClasses = new Dictionary<InterfaceTTSProviderEnum, Type>();

        public TTSProviderManager(ILogger<TTSProviderManager> logger, TTSProviderRepository ttsProviderRepository, IntegrationsManager integrationsManager)
        {
            _logger = logger;

            _ttsProviderRepository = ttsProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().Wait();
        }

        private async Task InitializeProvidersAsync()
        {
            foreach (InterfaceTTSProviderEnum providerEnum in Enum.GetValues(typeof(InterfaceTTSProviderEnum)))
            {
                if (providerEnum == InterfaceTTSProviderEnum.Unknown)
                    continue;

                var provider = await _ttsProviderRepository.GetProviderAsync(providerEnum);

                if (provider == null)
                {
                    await AddProvider(new TTSProviderData
                    {
                        Id = providerEnum,
                        DisabledAt = DateTime.UtcNow,
                        Models = new List<TTSProviderSpeakerData>(),
                        UserIntegrationFields = new List<ProviderFieldBase>()
                    });
                }
                else if (provider.DisabledAt == null)
                {
                    RegisterProviderService(providerEnum);
                }
            }
        }

        private void RegisterProviderService(InterfaceTTSProviderEnum providerEnum)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ttsServiceType = typeof(ITTSService);

            var matchingTypes = assembly.GetTypes()
                .Where(t => ttsServiceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();

            foreach (var type in matchingTypes)
            {
                var getProviderTypeMethod = type.GetMethod("GetProviderTypeStatic", BindingFlags.Public | BindingFlags.Static);
                if (getProviderTypeMethod != null)
                {
                    var returnedProviderEnum = (InterfaceTTSProviderEnum)getProviderTypeMethod.Invoke(null, null);
                    if (returnedProviderEnum == providerEnum)
                    {
                        _ttsProviderClasses[providerEnum] = type;
                        return;
                    }
                }
            }

            throw new Exception($"No matching ITTSService implementation found for provider: {providerEnum}");
        }

        public async Task<FunctionReturnResult<List<TTSProviderData>?>> GetProviderList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<TTSProviderData>?>();

            var providerList = await _ttsProviderRepository.GetProviderListAsync(page, pageSize);
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

        public async Task<FunctionReturnResult<TTSProviderData>> AddProvider(TTSProviderData providerData)
        {
            var result = new FunctionReturnResult<TTSProviderData>();

            if (providerData.Id == InterfaceTTSProviderEnum.Unknown)
            {
                result.Code = "AddProvider:1";
                result.Message = "Invalid provider ID";
                return result;
            }

            var existingProvider = await _ttsProviderRepository.GetProviderAsync(providerData.Id);
            if (existingProvider != null)
            {
                result.Code = "AddProvider:2";
                result.Message = "Provider already exists";
                return result;
            }

            providerData.DisabledAt = DateTime.UtcNow;
            await _ttsProviderRepository.AddProviderAsync(providerData);

            result.Success = true;
            result.Data = providerData;
            return result;
        }

        public Type? GetProviderService(InterfaceTTSProviderEnum providerId)
        {
            if (_ttsProviderClasses.TryGetValue(providerId, out var service))
            {
                return service;
            }
            return null;
        }

        public async Task<TTSProviderData?> GetProviderData(InterfaceTTSProviderEnum providerId)
        {
            return await _ttsProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<TTSProviderSpeakerData?>> AddUpdateProviderSpeaker(
            TTSProviderData provider,
            string speakerId,
            string postType,
            TTSProviderSpeakerData? oldSpeakerData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderSpeakerData?>();

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddUpdateProviderSpeaker:1";
                result.Message = "Changes data not found";
                return result;
            }

            try
            {
                var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString.ToString());
                if (changesJsonElement == null)
                {
                    result.Code = "AddUpdateProviderSpeaker:2";
                    result.Message = "Failed to parse changes JSON";
                    return result;
                }

                var newSpeakerData = new TTSProviderSpeakerData
                {
                    Id = speakerId,
                    DisabledAt = oldSpeakerData?.DisabledAt
                };

                // Parse and validate name
                if (!changesJsonElement.RootElement.TryGetProperty("name", out var nameElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:3";
                    result.Message = "Speaker name not found";
                    return result;
                }
                newSpeakerData.Name = nameElement.GetString() ?? "";

                // Parse and validate price settings
                if (!changesJsonElement.RootElement.TryGetProperty("pricePerUnit", out var priceElement) ||
                    !decimal.TryParse(priceElement.GetString(), out decimal price))
                {
                    result.Code = "AddUpdateProviderSpeaker:4";
                    result.Message = "Invalid price";
                    return result;
                }
                newSpeakerData.PricePerUnit = price;

                if (!changesJsonElement.RootElement.TryGetProperty("priceUnit", out var priceUnitElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:5";
                    result.Message = "Price unit not found";
                    return result;
                }
                newSpeakerData.PriceUnit = priceUnitElement.GetString() ?? "";

                // Parse speaker characteristics
                if (!changesJsonElement.RootElement.TryGetProperty("gender", out var genderElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:6";
                    result.Message = "Gender not found";
                    return result;
                }
                newSpeakerData.Gender = genderElement.GetString() ?? "";

                if (!changesJsonElement.RootElement.TryGetProperty("ageGroup", out var ageGroupElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:7";
                    result.Message = "Age group not found";
                    return result;
                }
                newSpeakerData.AgeGroup = ageGroupElement.GetString() ?? "";

                // Parse personality traits
                if (changesJsonElement.RootElement.TryGetProperty("personality", out var personalityElement))
                {
                    newSpeakerData.Personality = new List<string>();
                    foreach (var trait in personalityElement.EnumerateArray())
                    {
                        newSpeakerData.Personality.Add(trait.GetString() ?? "");
                    }
                }

                // Parse language settings
                if (!changesJsonElement.RootElement.TryGetProperty("isMultilingual", out var multilingualElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:8";
                    result.Message = "Multilingual setting not found";
                    return result;
                }
                newSpeakerData.IsMultilingual = multilingualElement.GetBoolean();

                if (!changesJsonElement.RootElement.TryGetProperty("supportedLanguages", out var languagesElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:9";
                    result.Message = "Supported languages not found";
                    return result;
                }

                newSpeakerData.SupportedLanguages = new List<string>();
                foreach (var language in languagesElement.EnumerateArray())
                {
                    newSpeakerData.SupportedLanguages.Add(language.GetString() ?? "");
                }

                // Parse speaking styles
                if (!changesJsonElement.RootElement.TryGetProperty("speakingStyles", out var stylesElement))
                {
                    result.Code = "AddUpdateProviderSpeaker:10";
                    result.Message = "Speaking styles not found";
                    return result;
                }

                newSpeakerData.SpeakingStyles = new List<TTSProviderSpeakingStyleData>();
                foreach (var styleElement in stylesElement.EnumerateArray())
                {
                    var style = new TTSProviderSpeakingStyleData
                    {
                        Id = styleElement.GetProperty("id").GetString() ?? "",
                        Name = styleElement.GetProperty("name").GetString() ?? "",
                        PreviewUrl = styleElement.GetProperty("previewUrl").GetString() ?? "",
                        IsDefault = styleElement.GetProperty("isDefault").GetBoolean()
                    };
                    newSpeakerData.SpeakingStyles.Add(style);
                }

                // Validate there's exactly one default style
                if (newSpeakerData.SpeakingStyles.Count(s => s.IsDefault) != 1)
                {
                    result.Code = "AddUpdateProviderSpeaker:11";
                    result.Message = "Exactly one speaking style must be set as default";
                    return result;
                }

                // Handle disabled state
                if (changesJsonElement.RootElement.TryGetProperty("disabled", out var disabledElement))
                {
                    bool disabled = disabledElement.GetBoolean();
                    newSpeakerData.DisabledAt = disabled ? DateTime.UtcNow : null;
                }

                // Save to database
                var updateResult = postType == "new" ?
                    await _ttsProviderRepository.AddSpeakerAsync(provider.Id, newSpeakerData) :
                    await _ttsProviderRepository.UpdateSpeakerAsync(provider.Id, newSpeakerData);

                if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
                {
                    result.Code = "AddUpdateProviderSpeaker:12";
                    result.Message = $"Failed to {postType} speaker";
                    return result;
                }

                result.Success = true;
                result.Data = newSpeakerData;
            }
            catch (Exception ex)
            {
                result.Code = "AddUpdateProviderSpeaker:13";
                result.Message = "Error processing speaker data: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<TTSProviderData?>> UpdateProvider(
            TTSProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    result.Code = "UpdateProvider:1";
                    result.Message = "Changes data not found";
                    return result;
                }

                var changesJsonElement = JsonSerializer.Deserialize<JsonDocument>(changesJsonString.ToString());
                if (changesJsonElement == null)
                {
                    result.Code = "UpdateProvider:2";
                    result.Message = "Unable to parse changes json string";
                    return result;
                }

                var newProviderData = new TTSProviderData
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
                newProviderData.DisabledAt = disabled ?
                    (provider.DisabledAt ?? DateTime.UtcNow) :
                    null;

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

                // Validate integration exists and is TTS type
                var integration = await integrationsManager.getIntegrationData(integrationId);
                if (integration == null || !integration.Success)
                {
                    result.Code = "UpdateProvider:6";
                    result.Message = "Selected integration not found";
                    return result;
                }

                if (!integration.Data.Type.Contains("TTS") && !integration.Data.Type.Contains("TEXT2SPEECH"))
                {
                    result.Code = "UpdateProvider:7";
                    result.Message = "Selected integration is not a TTS integration";
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
                var updateResult = await _ttsProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<TTSProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            try
            {
                var providerData = await _ttsProviderRepository.GetProviderDataByIntegration(integrationType);

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

        public async Task<FunctionReturnResult<ITTSService?>> BuildProviderServiceByIntegration(BusinessAppIntegration integrationData, BusinessAppAgentIntegrationData agentIntegrationData, Dictionary<string, string> metaData)
        {
            var result = new FunctionReturnResult<ITTSService?>();

            var ttsProviderData = await GetProviderDataByIntegration(integrationData.Type);
            if (!ttsProviderData.Success)
            {
                result.Code = "BuildProviderServiceByIntegration:1";
                result.Message = "Provider not find by integration type";
                return result;
            }

            switch (ttsProviderData.Data.Id)
            {
                case InterfaceTTSProviderEnum.AzureSpeechServices:
                    result.Success = true;
                    result.Data = new AzureSpeechTTSService(_integrationsManager.DecryptField(integrationData.EncryptedFields["resource_key"]), integrationData.Fields["resource_region"], (string)agentIntegrationData.FieldValues["speaker_language"], (string)agentIntegrationData.FieldValues["speaker"]);
                    return result;

                case InterfaceTTSProviderEnum.ElevenLabsTextToSpeech:
                    return result.SetSuccessResult(
                        new ElevenLabsTTSService(_integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]), (string)agentIntegrationData.FieldValues["model_id"], (string)agentIntegrationData.FieldValues["voice_id"], 0.5f, 0.5f, 0, false, 1.05f)
                    );

                default:
                    _logger.LogError("Business app TTS provider {ProviderType} not supported", ttsProviderData.Data.Id);
                    return result;
            }
        }
    }
}
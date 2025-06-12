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

            try
            {
                var ttsProviderData = await GetProviderDataByIntegration(integrationData.Type);
                if (!ttsProviderData.Success)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:RETRIEVE_PROVIDER_DATA_FAILED",
                        "Failed to retrieve provider data by integration type"
                    );
                }

                int sampleRate = 8000;

                switch (ttsProviderData.Data.Id)
                {
                    case InterfaceTTSProviderEnum.AzureSpeechServices:
                        {
                            string resourceKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["resource_key"]);
                            string resourceRegion = integrationData.Fields["resource_region"];
                            string speakerLanguage = (string)agentIntegrationData.FieldValues["speaker_language"];
                            string speakerName = (string)agentIntegrationData.FieldValues["speaker"];

                            var azureSpeechTTSService = new AzureSpeechTTSService(resourceKey, resourceRegion, speakerLanguage, speakerName, sampleRate);
                            return result.SetSuccessResult(azureSpeechTTSService);
                        }

                    case InterfaceTTSProviderEnum.ElevenLabsTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string modelId = (string)agentIntegrationData.FieldValues["model_id"];
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            float stability = (float)(double)agentIntegrationData.FieldValues["stability"];
                            float similarityBoost = (float)(double)agentIntegrationData.FieldValues["similarityBoost"];
                            float style = (float)(double)agentIntegrationData.FieldValues["style"];
                            bool speakerBoost = bool.Parse((string)agentIntegrationData.FieldValues["speakerBoost"]);
                            float speed = (float)(double)agentIntegrationData.FieldValues["speed"];
                            string pronunciationDictionaryId = (string)agentIntegrationData.FieldValues["pronunciationDictionaryId"];
                            string applyTextNormalization = (string)agentIntegrationData.FieldValues["applyTextNormalization"];

                            var elevenLabsTTSService = new ElevenLabsTTSService(apiKey, modelId, voiceId, sampleRate, stability, similarityBoost, style, speakerBoost, speed, pronunciationDictionaryId, applyTextNormalization);
                            return result.SetSuccessResult(elevenLabsTTSService);
                        }

                    case InterfaceTTSProviderEnum.GoogleCloudTextToSpeech:
                        {
                            string serviceAccountKeyJson = _integrationsManager.DecryptField(integrationData.EncryptedFields["service_account_key_json"]);
                            string languageCode = (string)agentIntegrationData.FieldValues["language_code"];
                            string voiceName = (string)agentIntegrationData.FieldValues["voice_name"];
                            float speakingRate = (float)(double)agentIntegrationData.FieldValues["speaking_rate"];

                            var googleTTSService = new GoogleTTSService(serviceAccountKeyJson, languageCode, voiceName, speakingRate, sampleRate);
                            return result.SetSuccessResult(googleTTSService);
                        }

                    case InterfaceTTSProviderEnum.CartesiaTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            string modelId = (string)agentIntegrationData.FieldValues["model_id"];
                            string languageCode = (string)agentIntegrationData.FieldValues["language_code"];
                            List<string> pronunciationDictIds = ((string)agentIntegrationData.FieldValues["pronunciationDictIds"]).Split(',').ToList();

                            var cartesiaTTSService = new CartesiaTTSService(apiKey, voiceId, modelId, languageCode, pronunciationDictIds, sampleRate);
                            return result.SetSuccessResult(cartesiaTTSService);
                        }

                    case InterfaceTTSProviderEnum.FishAudioTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string referenceId = (string)agentIntegrationData.FieldValues["reference_id"];
                            string model = (string)agentIntegrationData.FieldValues["model"];

                            var fishAudioTTSService = new FishAudioTTSService(apiKey, referenceId, model, sampleRate);
                            return result.SetSuccessResult(fishAudioTTSService);
                        }

                    case InterfaceTTSProviderEnum.DeepgramTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string modelId = (string)agentIntegrationData.FieldValues["model_id"];

                            var deepgramTTSService = new DeepgramTTSService(apiKey, modelId, sampleRate);
                            return result.SetSuccessResult(deepgramTTSService);
                        }

                    case InterfaceTTSProviderEnum.MinimaxTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string groupId = (string)agentIntegrationData.FieldValues["group_id"];
                            string modelId = (string)agentIntegrationData.FieldValues["model_id"];
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            float voiceSpeed = (float)(double)agentIntegrationData.FieldValues["voice_speed"];
                            string languageBoostId = (string)agentIntegrationData.FieldValues["language_boost_id"];
                            string pronunciationDict = (string)agentIntegrationData.FieldValues["pronunciation_dict"];

                            var minimaxTTSService = new MinimaxTTSService(apiKey, groupId, modelId, voiceId, voiceSpeed, languageBoostId, pronunciationDict, sampleRate);
                            return result.SetSuccessResult(minimaxTTSService);
                        }

                    case InterfaceTTSProviderEnum.HumeAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            string voiceProvider = (string)agentIntegrationData.FieldValues["voice_provider"];
                            string voiceDescription = (string)agentIntegrationData.FieldValues["voice_description"];
                            float voiceSpeed = (float)(double)agentIntegrationData.FieldValues["voice_speed"];

                            var humeAITTSService = new HumeAITTSService(apiKey, voiceId, voiceProvider, voiceDescription, voiceSpeed, sampleRate);
                            return result.SetSuccessResult(humeAITTSService);
                        }

                    case InterfaceTTSProviderEnum.PlayHtTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string userId = _integrationsManager.DecryptField(integrationData.EncryptedFields["user_id"]);
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            string voiceEngine = (string)agentIntegrationData.FieldValues["voice_engine"];
                            string voiceQuality = (string)agentIntegrationData.FieldValues["voice_quality"];
                            float voiceSpeed = (float)(double)agentIntegrationData.FieldValues["voice_speed"];
                            float temperature = (float)(double)agentIntegrationData.FieldValues["temperature"];
                            string emotion = (string)agentIntegrationData.FieldValues["emotion"];
                            float voiceGuidance = (float)(double)agentIntegrationData.FieldValues["voice_guidance"];
                            float styleGuidance = (float)(double)agentIntegrationData.FieldValues["style_guidance"];
                            float textGuidance = (float)(double)agentIntegrationData.FieldValues["text_guidance"];
                            string language = (string)agentIntegrationData.FieldValues["language"];

                            var playHtTTSService = new PlayHtTTSService(apiKey, userId, voiceId, voiceEngine, voiceQuality, voiceSpeed, temperature, emotion, voiceGuidance, styleGuidance, textGuidance, language, sampleRate);
                            return result.SetSuccessResult(playHtTTSService);
                        }

                    case InterfaceTTSProviderEnum.SpeechifyTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            string model = (string)agentIntegrationData.FieldValues["model"];
                            string language = (string)agentIntegrationData.FieldValues["language"];
                            string loudnessNormalizationString = (string)agentIntegrationData.FieldValues["loudness_normalization"];
                            string textNormalizationString = (string)agentIntegrationData.FieldValues["text_normalization"];

                            bool loudnessNormalization = loudnessNormalizationString == "true" ? true : false;
                            bool textNormalization = textNormalizationString == "true" ? true : false;

                            var speechifyTTSService = new SpeechifyTTSService(apiKey, voiceId, model, language, loudnessNormalization, textNormalization, sampleRate);
                            return result.SetSuccessResult(speechifyTTSService);
                        }

                    case InterfaceTTSProviderEnum.MurfAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string model = (string)agentIntegrationData.FieldValues["model"];
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            string multiNativeLocale = (string)agentIntegrationData.FieldValues["multi_native_locale"];
                            string pronunciationDictionaryString = (string)agentIntegrationData.FieldValues["pronunciation_dictionary"];
                            int rate = (int)agentIntegrationData.FieldValues["rate"];
                            string style = (string)agentIntegrationData.FieldValues["style"];
                            int variation = (int)agentIntegrationData.FieldValues["variation"];

                            var murfAITTSService = new MurfAITTSService(apiKey, model, voiceId, multiNativeLocale, pronunciationDictionaryString, rate, style, variation, sampleRate);
                            return result.SetSuccessResult(murfAITTSService);
                        }

                    case InterfaceTTSProviderEnum.ZyphraZonosTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string model = (string)agentIntegrationData.FieldValues["model"];
                            string defaultVoiceName = (string)agentIntegrationData.FieldValues["default_voice_name"];
                            float speakingRate = (float)(double)agentIntegrationData.FieldValues["speaking_rate"];
                            string languageIsoCode = (string)agentIntegrationData.FieldValues["language_iso_code"];
                            string emotion = (string)agentIntegrationData.FieldValues["emotion"];
                            float vqscore = (float)(double)agentIntegrationData.FieldValues["vqscore"];

                            var zyphraZonosTTSService = new ZyphraZonosTTSService(apiKey, model, defaultVoiceName, speakingRate, languageIsoCode, emotion, vqscore, sampleRate);
                            return result.SetSuccessResult(zyphraZonosTTSService);
                        }

                    case InterfaceTTSProviderEnum.ResembleAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string projectUuid = integrationData.Fields["project_uuid"];
                            string voiceUuid = (string)agentIntegrationData.FieldValues["voice_uuid"];

                            var resembleAiTTSService = new ResembleAITTSService(apiKey, projectUuid, voiceUuid, sampleRate);
                            return result.SetSuccessResult(resembleAiTTSService);
                        }

                    case InterfaceTTSProviderEnum.HamsaAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string speaker = (string)agentIntegrationData.FieldValues["speaker"];
                            string dialect = (string)agentIntegrationData.FieldValues["dialect"];

                            var hamsaTTSService = new HamsaAITTSService(apiKey, speaker, dialect, sampleRate);
                            return result.SetSuccessResult(hamsaTTSService);
                        }

                    case InterfaceTTSProviderEnum.NeuphonicTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string langCode = (string)agentIntegrationData.FieldValues["lang_code"];
                            string voiceId = (string)agentIntegrationData.FieldValues["voice_id"];
                            float speed = (float)(double)agentIntegrationData.FieldValues["speed"];
                            string model = (string)agentIntegrationData.FieldValues["model"];

                            var neuphonicTTSService = new NeuphonicTTSService(apiKey, langCode, model, voiceId, speed, sampleRate);
                            return result.SetSuccessResult(neuphonicTTSService);
                        }

                    default:
                        {
                            _logger.LogError("Business app TTS provider {ProviderType} not supported", ttsProviderData.Data.Id);
                            return result.SetFailureResult(
                                "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                                ("Provider not supported: " + ttsProviderData.Data.Id)
                            );
                        }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    ("Failed to build provider service: " + ex.Message)
                );
            }    
        }
    }
}
using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.AzureSpeech;
using IqraCore.Entities.TTS.Providers.Cartesia;
using IqraCore.Entities.TTS.Providers.Deepgram;
using IqraCore.Entities.TTS.Providers.ElevenLabs;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Entities.TTS.Providers.Google;
using IqraCore.Entities.TTS.Providers.Hamsa;
using IqraCore.Entities.TTS.Providers.HumeAI;
using IqraCore.Entities.TTS.Providers.Minimax;
using IqraCore.Entities.TTS.Providers.MurfAI;
using IqraCore.Entities.TTS.Providers.Neuphonic;
using IqraCore.Entities.TTS.Providers.PlayHt;
using IqraCore.Entities.TTS.Providers.ResembleAI;
using IqraCore.Entities.TTS.Providers.Speechify;
using IqraCore.Entities.TTS.Providers.ZyphraZonos;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.TTS.Providers;
using IqraInfrastructure.Repositories.TTS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

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

        public async Task<FunctionReturnResult<ITTSService?>> BuildProviderServiceByIntegration(ILoggerFactory loggerFactory, BusinessAppIntegration integrationData, BusinessAppAgentIntegrationData agentIntegrationData, int targetSampleRate, int targetBitsPerSample, AudioEncodingTypeEnum targetAudioEncoding)
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

                switch (ttsProviderData.Data.Id)
                {
                    case InterfaceTTSProviderEnum.AzureSpeechServices:
                        {
                            string tenantId = integrationData.Fields["tenant_id"];
                            string clientId = integrationData.Fields["client_id"];
                            string clientSecret = _integrationsManager.DecryptField(integrationData.EncryptedFields["client_secret"]);
                            string subscriptionId = integrationData.Fields["subscription_id"];
                            string resourceGroupName = integrationData.Fields["resource_group_name"];
                            string speechResourceName = integrationData.Fields["speech_resource_name"];
                            string resourceRegion = integrationData.Fields["resource_region"];

                            var config = new AzureSpeechConfig
                            {
                                Language = (string)agentIntegrationData.FieldValues["speaker_language"],
                                VoiceName = (string)agentIntegrationData.FieldValues["speaker"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new AzureSpeechTTSService(tenantId, clientId, clientSecret, subscriptionId, resourceGroupName, speechResourceName, resourceRegion, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.ElevenLabsTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new ElevenLabsConfig
                            {
                                ModelId = (string)agentIntegrationData.FieldValues["model_id"],
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                Stability = Convert.ToSingle(agentIntegrationData.FieldValues["stability"]),
                                SimilarityBoost = Convert.ToSingle(agentIntegrationData.FieldValues["similarityBoost"]),
                                Style = Convert.ToSingle(agentIntegrationData.FieldValues["style"]),
                                UseSpeakerBoost = Convert.ToBoolean(agentIntegrationData.FieldValues["speakerBoost"]),
                                Speed = Convert.ToSingle(agentIntegrationData.FieldValues["speed"]),
                                PronunciationDictionaryId = (string)agentIntegrationData.FieldValues["pronunciationDictionaryId"],
                                ApplyTextNormalization = (string)agentIntegrationData.FieldValues["applyTextNormalization"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ElevenLabsTTSService(loggerFactory.CreateLogger<ElevenLabsTTSService>(), apiKey, config); // Assuming constructor is updated
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.GoogleCloudTextToSpeech:
                        {
                            string serviceAccountKeyJson = _integrationsManager.DecryptField(integrationData.EncryptedFields["service_account_key_json"]);
                            string projectId = integrationData.Fields["project_id"];

                            var config = new GoogleConfig
                            {
                                LanguageCode = (string)agentIntegrationData.FieldValues["language_code"],
                                VoiceName = (string)agentIntegrationData.FieldValues["voice_name"],
                                SpeakingRate = (float)(double)agentIntegrationData.FieldValues["speaking_rate"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new GoogleTTSService(loggerFactory.CreateLogger<GoogleTTSService>(), projectId, serviceAccountKeyJson, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.CartesiaTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new CartesiaConfig
                            {
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                ModelId = (string)agentIntegrationData.FieldValues["model_id"],
                                LanguageCode = (string)agentIntegrationData.FieldValues["language_code"],
                                PronunciationDictIds = ((string)agentIntegrationData.FieldValues["pronunciationDictIds"]).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new CartesiaTTSService(loggerFactory.CreateLogger<CartesiaTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.FishAudioTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new FishAudioConfig
                            {
                                ReferenceId = (string)agentIntegrationData.FieldValues["reference_id"],
                                Model = (string)agentIntegrationData.FieldValues["model"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new FishAudioTTSService(apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.DeepgramTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new DeepgramConfig
                            {
                                ModelId = (string)agentIntegrationData.FieldValues["model_id"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new DeepgramTTSService(loggerFactory.CreateLogger<DeepgramTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.MinimaxTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new MinimaxConfig
                            {
                                ModelId = (string)agentIntegrationData.FieldValues["model_id"],
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                VoiceSpeed = (float)(double)agentIntegrationData.FieldValues["voice_speed"],
                                LanguageBoost = (string)agentIntegrationData.FieldValues["language_boost"],
                                //PronunciationDict = (string)agentIntegrationData.FieldValues["pronunciation_dict"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new MinimaxTTSService(loggerFactory.CreateLogger<MinimaxTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.HumeAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new HumeAiConfig
                            {
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                VoiceProvider = (string)agentIntegrationData.FieldValues["voice_provider"],
                                VoiceDescription = (string)agentIntegrationData.FieldValues["voice_description"],
                                VoiceSpeed = (float)(double)agentIntegrationData.FieldValues["voice_speed"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new HumeAITTSService(loggerFactory.CreateLogger<HumeAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.SpeechifyTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new SpeechifyConfig
                            {
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                Model = (string)agentIntegrationData.FieldValues["model"],
                                Language = (string)agentIntegrationData.FieldValues["language"],
                                LoudnessNormalization = bool.Parse((string)agentIntegrationData.FieldValues["loudness_normalization"]),
                                TextNormalization = bool.Parse((string)agentIntegrationData.FieldValues["text_normalization"]),
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new SpeechifyTTSService(loggerFactory.CreateLogger<SpeechifyTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.MurfAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new MurfAiConfig
                            {
                                Model = (string)agentIntegrationData.FieldValues["model"],
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                MultiNativeLocale = (string)agentIntegrationData.FieldValues["multi_native_locale"],
                                PronunciationDictionaryString = (string)agentIntegrationData.FieldValues["pronunciation_dictionary"],
                                Rate = (int)(long)agentIntegrationData.FieldValues["rate"], // JSON deserializes numbers to long by default
                                Style = (string)agentIntegrationData.FieldValues["style"],
                                Variation = (int)(long)agentIntegrationData.FieldValues["variation"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new MurfAITTSService(loggerFactory.CreateLogger<MurfAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.ZyphraZonosTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            // Emotion dictionary needs to be parsed
                            var emotionDict = ((string)agentIntegrationData.FieldValues["emotion"])
                                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                                .Select(part => part.Split(':'))
                                .Where(parts => parts.Length == 2)
                                .ToDictionary(parts => parts[0], parts => float.Parse(parts[1]));

                            var config = new ZyphraZonosConfig
                            {
                                Model = (string)agentIntegrationData.FieldValues["model"],
                                DefaultVoiceName = (string)agentIntegrationData.FieldValues["default_voice_name"],
                                SpeakingRate = (int)(long)agentIntegrationData.FieldValues["speaking_rate"],
                                LanguageIsoCode = (string)agentIntegrationData.FieldValues["language_iso_code"],
                                Emotion = emotionDict,
                                Vqscore = (float)(double)agentIntegrationData.FieldValues["vqscore"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ZyphraZonosTTSService(loggerFactory.CreateLogger<ZyphraZonosTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.ResembleAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string projectUuid = integrationData.Fields["project_uuid"];

                            var config = new ResembleAiConfig
                            {
                                ProjectUuid = projectUuid,
                                VoiceUuid = (string)agentIntegrationData.FieldValues["voice_uuid"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ResembleAITTSService(loggerFactory.CreateLogger<ResembleAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.HamsaAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new HamsaAiConfig
                            {
                                Speaker = (string)agentIntegrationData.FieldValues["speaker"],
                                Dialect = (string)agentIntegrationData.FieldValues["dialect"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new HamsaAITTSService(loggerFactory.CreateLogger<HamsaAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.NeuphonicTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new NeuphonicConfig
                            {
                                LanguageCode = (string)agentIntegrationData.FieldValues["lang_code"],
                                Model = (string)agentIntegrationData.FieldValues["model"],
                                VoiceId = (string)agentIntegrationData.FieldValues["voice_id"],
                                Speed = (float)(double)agentIntegrationData.FieldValues["speed"],
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new NeuphonicTTSService(loggerFactory.CreateLogger<NeuphonicTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
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
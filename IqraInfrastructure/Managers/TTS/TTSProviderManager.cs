using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.TTS;
using IqraCore.Entities.TTS.Providers.AzureSpeech;
using IqraCore.Entities.TTS.Providers.Cartesia;
using IqraCore.Entities.TTS.Providers.Deepgram;
using IqraCore.Entities.TTS.Providers.ElevenLabs;
using IqraCore.Entities.TTS.Providers.FishAudio;
using IqraCore.Entities.TTS.Providers.Google;
using IqraCore.Entities.TTS.Providers.Hamsa;
using IqraCore.Entities.TTS.Providers.HumeAI;
using IqraCore.Entities.TTS.Providers.Inworld;
using IqraCore.Entities.TTS.Providers.Minimax;
using IqraCore.Entities.TTS.Providers.MurfAI;
using IqraCore.Entities.TTS.Providers.Neuphonic;
using IqraCore.Entities.TTS.Providers.ResembleAI;
using IqraCore.Entities.TTS.Providers.Rime;
using IqraCore.Entities.TTS.Providers.Sarvam;
using IqraCore.Entities.TTS.Providers.Speechify;
using IqraCore.Entities.TTS.Providers.UpliftAI;
using IqraCore.Entities.TTS.Providers.ZyphraZonos;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Provider;
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

        public TTSProviderManager(
            ILogger<TTSProviderManager> logger,
            TTSProviderRepository ttsProviderRepository,
            IntegrationsManager integrationsManager)
        {
            _logger = logger;
            _ttsProviderRepository = ttsProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().GetAwaiter().GetResult();
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
                    var addResult = await AddProvider(providerEnum);
                    if (!addResult.Success)
                    {
                        throw new Exception($"Failed to add tts provider: {providerEnum}: [{addResult.Code}] {addResult.Message}");
                    }
                }

                RegisterProviderService(providerEnum);
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
                    var returnedProviderEnum = (InterfaceTTSProviderEnum)getProviderTypeMethod.Invoke(null, null)!;
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

            try
            {
                var providerList = await _ttsProviderRepository.GetProviderListAsync(page, pageSize);
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

        public async Task<FunctionReturnResult<TTSProviderData>> AddProvider(InterfaceTTSProviderEnum providerId)
        {
            var result = new FunctionReturnResult<TTSProviderData>();

            try
            {
                var providerData = new TTSProviderData()
                {
                    Id = providerId,
                    DisabledAt = DateTime.UtcNow,
                };

                if (providerData.Id == InterfaceTTSProviderEnum.Unknown)
                {
                    return result.SetFailureResult(
                        "AddProvider:INVALID_ID",
                        "Invalid provider ID"
                    );
                }

                var existingProvider = await _ttsProviderRepository.GetProviderAsync(providerData.Id);
                if (existingProvider != null)
                {
                    return result.SetFailureResult(
                        "AddProvider:EXISTS",
                        "Provider already exists"
                    );
                }

                var success = await _ttsProviderRepository.AddProviderAsync(providerData);
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

        public Type? GetProviderService(InterfaceTTSProviderEnum providerId)
        {
            return _ttsProviderClasses.TryGetValue(providerId, out var service) ? service : null;
        }

        public async Task<TTSProviderData?> GetProviderData(InterfaceTTSProviderEnum providerId)
        {
            return await _ttsProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<TTSProviderData?>> UpdateProvider(
            TTSProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

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
                var newProviderData = new TTSProviderData
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

                    // Validate integration exists and is TTS type
                    var integration = await integrationsManager.getIntegrationData(integrationId);
                    if (integration.Data == null || !integration.Success)
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:SELECTED_INTEGRATION_NOT_FOUND",
                            "Selected integration not found"
                        );
                    }

                    if (!integration.Data.Type.Contains("TTS") && !integration.Data.Type.Contains("TEXT2SPEECH"))
                    {
                        return result.SetFailureResult(
                            "UpdateProvider:INVALID_INTEGRATION",
                            "Selected integration is not a TTS integration"
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
                var updateResult = await _ttsProviderRepository.UpdateProviderAsync(newProviderData);
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

        public async Task<FunctionReturnResult<TTSProviderModelData?>> AddUpdateProviderModel(
            TTSProviderData provider,
            string modelId,
            string postType,
            TTSProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<TTSProviderModelData?>();

            try
            {
                if (!formData.TryGetValue("changes", out var changesJsonString))
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
                var newModelData = new TTSProviderModelData
                {
                    Id = modelId,
                };

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

                // Price Per Unit
                if (root.TryGetProperty("pricePerUnit", out var priceElement))
                {
                    // Use string parsing for safety with decimals in JSON
                    if (decimal.TryParse(priceElement.GetString(), out decimal price))
                    {
                        newModelData.PricePerUnit = price;
                    }
                    else if (priceElement.TryGetDecimal(out decimal decimalVal))
                    {
                        newModelData.PricePerUnit = decimalVal;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:INVALID_PRICE",
                            "Invalid price format"
                        );
                    }
                }

                // Price Unit
                if (root.TryGetProperty("priceUnit", out var priceUnitElement))
                {
                    string? priceUnit = priceUnitElement.GetString();
                    if (string.IsNullOrEmpty(priceUnit))
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:EMPTY_PRICE_UNIT",
                            "Price unit is required"
                        );
                    }
                    newModelData.PriceUnit = priceUnit;
                }

                // Supported Languages
                if (root.TryGetProperty("supportedLanguages", out var languagesElement) && languagesElement.ValueKind == JsonValueKind.Array)
                {
                    newModelData.SupportedLanguages = new List<string>();
                    foreach (var language in languagesElement.EnumerateArray())
                    {
                        newModelData.SupportedLanguages.Add(language.GetString() ?? "");
                    }
                }
                else
                {
                    return result.SetFailureResult(
                        "AddUpdateProviderModel:LANGUAGES_NOT_FOUND",
                        "Supported languages not found or invalid format"
                    );
                }

                // Save to database
                bool updateSuccess;
                if (postType == "new")
                {
                    var addResult = await _ttsProviderRepository.AddModelAsync(provider.Id, newModelData);
                    updateSuccess = addResult.IsAcknowledged && addResult.ModifiedCount > 0;
                }
                else
                {
                    var updateResult = await _ttsProviderRepository.UpdateModelAsync(provider.Id, newModelData);
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

        public async Task<FunctionReturnResult<TTSProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<TTSProviderData?>();

            try
            {
                var providerData = await _ttsProviderRepository.GetProviderDataByIntegration(integrationType);

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

        public async Task<FunctionReturnResult<ITTSService?>> BuildProviderServiceByIntegration(
            ILoggerFactory loggerFactory,
            BusinessAppIntegration integrationData,
            BusinessAppAgentIntegrationData agentIntegrationData,
            int targetSampleRate,
            int targetBitsPerSample,
            AudioEncodingTypeEnum targetAudioEncoding)
        {
            var result = new FunctionReturnResult<ITTSService?>();

            try
            {
                var ttsProviderData = await GetProviderDataByIntegration(integrationData.Type);
                if (!ttsProviderData.Success || ttsProviderData.Data == null)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:RETRIEVE_PROVIDER_DATA_FAILED",
                        "Failed to retrieve provider data by integration type"
                    );
                }

                // --- Helper functions for safe extraction ---
                string? GetString(string key, string? defaultValue = null)
                {
                    return agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null
                        ? val.ToString()! : defaultValue;
                }

                int? GetInt(string key, int? defaultValue = null)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        if (int.TryParse(val.ToString(), out int parsed)) return parsed;
                        return Convert.ToInt32(val);
                    }
                    return defaultValue;
                }

                long? GetLong(string key, long? defaultValue = null)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        if (long.TryParse(val.ToString(), out long parsed)) return parsed;
                        return Convert.ToInt64(val);
                    }
                    return defaultValue;
                }

                double? GetDouble(string key, double? defaultValue = null)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        if (double.TryParse(val.ToString(), out double parsed)) return parsed;
                        return Convert.ToDouble(val);
                    }
                    return defaultValue;
                }

                float? GetFloat(string key, float? defaultValue = null)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        if (float.TryParse(val.ToString(), out float parsed)) return parsed;
                        return Convert.ToSingle(val);
                    }
                    return defaultValue;
                }

                bool GetBool(string key, bool defaultValue)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        var s = val.ToString()!.ToLower();
                        if (s == "on" || s == "yes" || s == "true") return true;
                        if (s == "off" || s == "no" || s == "false") return false;
                    }
                    return defaultValue;
                }

                List<string>? GetList(string key, List<string>? defaultValue = null)
                {
                    if (agentIntegrationData.FieldValues.TryGetValue(key, out var val) && val != null)
                    {
                        var s = val.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            return s.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                        }
                    }
                    return defaultValue;
                }
                // ---------------------------------------------

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
                                Language = GetString("speaker_language")!,
                                VoiceName = GetString("speaker")!,
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
                                ModelId = GetString("model_id")!,
                                VoiceId = GetString("voice_id")!,
                                LanguageCode = GetString("language_code"),
                                Stability = GetFloat("stability"),
                                SimilarityBoost = GetFloat("similarityBoost"),
                                Style = GetFloat("style"),
                                UseSpeakerBoost = GetBool("speakerBoost", false),
                                Speed = GetFloat("speed"),
                                PronunciationDictionaryIds = GetList("pronunciationDictionaryId"),
                                ApplyTextNormalization = GetString("applyTextNormalization"),
                                UsePreviousRequestIds = GetBool("use_previous_request_ids", false),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ElevenLabsTTSService(loggerFactory.CreateLogger<ElevenLabsTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.GoogleCloudTextToSpeech:
                        {
                            string serviceAccountKeyJson = _integrationsManager.DecryptField(integrationData.EncryptedFields["service_account_key_json"]);

                            var config = new GoogleTTSConfig
                            {
                                ModelType = GetString("model_type")!,
                                LanguageCode = GetString("language_code")!,
                                // gemini
                                GeminiModelId = GetString("model_id"),
                                Prompt = GetString("prompt"),
                                // chirp
                                VoiceName = GetString("voice_name"),
                                UseCustomVoiceKey = GetBool("use_custom_voice_key", false),
                                VoiceCloningKey = GetString("voice_cloning_key"),
                                // common
                                CustomPronunciationsJson = GetString("custom_pronunciations"),
                                SpeakingRate = GetDouble("speaking_rate"),
                                Pitch = GetDouble("pitch"),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new GoogleTTSService(loggerFactory.CreateLogger<GoogleTTSService>(), serviceAccountKeyJson, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.CartesiaTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new CartesiaConfig
                            {
                                ModelId = GetString("model_id")!,
                                VoiceId = GetString("voice_id")!,
                                LanguageCode = GetString("language_code")!,
                                Volume = GetDouble("volume"),
                                Speed = GetDouble("speed"),
                                Emotion = GetString("emotion"),
                                PronunciationDictId = GetString("pronunciation_dict_id"),

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
                                Model = GetString("model")!,
                                ReferenceId = GetString("reference_id")!,
                                Temperature = GetFloat("temperature"),
                                TopP = GetFloat("top_p"),
                                Speed = GetFloat("speed"),
                                Volume = GetFloat("volume"),
                                Latency = GetString("latency"),
                                Normalize = GetBool("normalize", true),
                                RepetitionPenalty = GetFloat("repetition_penalty"),
                                ChunkLength = GetInt("chunk_length"),
                                MaxNewTokens = GetInt("max_new_tokens"),

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
                            bool mipOptOut = GetBool("mip_opt_out", false);

                            var config = new DeepgramConfig
                            {
                                ModelFamily = GetString("model_family")!,
                                VoiceName = GetString("voice_name")!,
                                LanguageCode = GetString("language_code")!,

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new DeepgramTTSService(loggerFactory.CreateLogger<DeepgramTTSService>(), apiKey, config, mipOptOut);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.MinimaxTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new MinimaxConfig
                            {
                                ModelId = GetString("model_id")!,
                                VoiceId = GetString("voice_id")!,
                                LanguageBoost = GetString("language_boost")!,
                                VoiceSpeed = GetFloat("voice_speed"),
                                VoiceVolume = GetFloat("voice_volume"),
                                VoicePitch = GetInt("voice_pitch"),
                                VoiceEmotions = GetString("voice_emotions"),
                                VoiceTextNormalization = GetBool("text_normalization", false),
                                PronunciationDictTones = GetList("pronunciation_dict_tones"),
                                VoiceModifyPitch = GetInt("voice_modify_pitch"),
                                VoiceModifyIntensity = GetInt("voice_modify_intensity"),
                                VoiceModifyTimbre = GetInt("voice_modify_timbre"),
                                VoiceModifySoundEffects = GetString("voice_modify_sound_effects"),

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
                                ModelVersion = (int)GetInt("model_version")!,
                                VoiceId = GetString("voice_id"),
                                VoiceProvider = GetString("voice_provider"),
                                VoiceDescription = GetString("voice_description"),
                                VoiceSpeed = GetFloat("voice_speed"),
                                InstantMode = GetBool("instant_mode", true),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new HumeAITTSService(loggerFactory.CreateLogger<HumeAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.InworldTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new InworldConfig
                            {
                                Model = GetString("model")!,
                                VoiceName = GetString("voice_name")!,
                                Speed = GetDouble("speed"),
                                Temperature = GetDouble("temperature"),
                                ApplyTextNormalization = GetString("apply_text_normalization"),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new InworldTTSService(loggerFactory.CreateLogger<InworldTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.SpeechifyTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new SpeechifyConfig
                            {
                                Model = GetString("model")!,
                                VoiceId = GetString("voice_id")!,
                                Language = GetString("language"),
                                LoudnessNormalization = GetBool("loudness_normalization", false),
                                TextNormalization = GetBool("text_normalization", true),

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
                                Model = GetString("model")!,
                                Region = GetString("region")!,
                                VoiceId = GetString("voice_id")!,
                                MultiNativeLocale = GetString("multi_native_locale"),
                                Style = GetString("style"),
                                Rate = GetInt("rate"),
                                Pitch = GetInt("pitch"),
                                Variation = GetInt("variation"),
                                PronunciationDictionaryJson = GetString("pronunciation_dictionary"),

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

                            var config = new ZyphraZonosConfig
                            {
                                Model = GetString("model")!,
                                DefaultVoiceName = GetString("default_voice_name")!,
                                LanguageIsoCode = GetString("language_iso_code")!,
                                SpeakingRate = GetInt("speaking_rate"),
                                Vqscore = GetFloat("vqscore"),
                                Fmax = GetFloat("fmax"),

                                // Model-specific fields
                                EmotionJson = GetString("emotion"),
                                PitchStd = GetFloat("pitch_std"),
                                SpeakerNoised = GetBool("speaker_noised", false),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ZyphraZonosTTSService(loggerFactory.CreateLogger<ZyphraZonosTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.HamsaAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new HamsaAiConfig
                            {
                                Speaker = GetString("speaker")!,
                                Dialect = GetString("dialect")!,

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
                                LanguageCode = GetString("lang_code")!,
                                VoiceId = GetString("voice_id")!,
                                Speed = GetFloat("speed"),
                                Temperature = GetFloat("temperature"),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new NeuphonicTTSService(loggerFactory.CreateLogger<NeuphonicTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.ResembleAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);
                            string? projectUuid = integrationData.Fields.TryGetValue("project_uuid", out var projectUuidValue) ? projectUuidValue : null;

                            var config = new ResembleAiConfig
                            {
                                Model = GetString("model")!,
                                VoiceUuid = GetString("voice_uuid")!,
                                UseHd = GetBool("use_hd", false),
                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new ResembleAITTSService(loggerFactory.CreateLogger<ResembleAITTSService>(), projectUuid, apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.UpliftAITextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new UpliftAiConfig
                            {
                                VoiceId = GetString("voice_id")!,
                                PhraseReplacementConfigId = GetString("phrase_replacement_config_id"),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new UpliftAITTSService(loggerFactory.CreateLogger<UpliftAITTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.SarvamTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new SarvamConfig
                            {
                                Model = GetString("model")!,
                                TargetLanguageCode = GetString("target_language_code")!,
                                Speaker = GetString("speaker")!,
                                Pitch = GetFloat("pitch"),
                                Loudness = GetFloat("loudness"),
                                EnablePreprocessing = GetBool("enable_preprocessing", false),
                                PaceV2 = GetFloat("pace_v2"),
                                PaceV3 = GetFloat("pace_v3"),
                                Temperature = GetFloat("temperature"),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new SarvamTTSService(loggerFactory.CreateLogger<SarvamTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    case InterfaceTTSProviderEnum.RimeTextToSpeech:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            var config = new RimeConfig
                            {
                                ModelId = GetString("model_id")!,
                                Speaker = GetString("speaker")!,
                                Lang = GetString("lang")!,
                                SpeedAlpha = GetDouble("speed_alpha"),

                                // Arcana
                                MaxTokens = GetInt("max_tokens"),
                                RepetitionPenalty = GetDouble("repetition_penalty"),
                                Temperature = GetDouble("temperature"),
                                TopP = GetDouble("top_p"),

                                // Mist
                                PauseBetweenBrackets = GetBool("pause_between_brackets", false),
                                PhonemizeBetweenBrackets = GetBool("phonemize_between_brackets", false),
                                InlineSpeedAlpha = GetString("inline_speed_alpha"),
                                NoTextNormalization = GetBool("no_text_normalization", false),
                                SaveOovs = GetBool("save_oovs", false),

                                TargetSampleRate = targetSampleRate,
                                TargetBitsPerSample = targetBitsPerSample,
                                TargetEncodingType = targetAudioEncoding
                            };

                            var service = new RimeTTSService(loggerFactory.CreateLogger<RimeTTSService>(), apiKey, config);
                            return result.SetSuccessResult(service);
                        }

                    default:
                        {
                            _logger.LogError("Business app TTS provider {ProviderType} not supported", ttsProviderData.Data.Id);
                            return result.SetFailureResult(
                                "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                                $"Provider {ttsProviderData.Data.Id} not supported"
                            );
                        }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "BuildProviderServiceByIntegration:EXCEPTION",
                    $"Failed to build provider service: {ex.Message}"
                );
            }
        }
    }
}
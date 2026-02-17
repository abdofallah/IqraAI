using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Audio;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Interfaces.AI;
using IqraInfrastructure.Helpers.Provider;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.STT.Providers;
using IqraInfrastructure.Repositories.STT;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace IqraInfrastructure.Managers.STT
{
    public class STTProviderManager
    {
        private readonly ILogger<STTProviderManager> _logger;
        private readonly STTProviderRepository _sttProviderRepository;
        private readonly IntegrationsManager _integrationsManager;

        private readonly Dictionary<InterfaceSTTProviderEnum, Type> _sttProviderClasses = new();

        public STTProviderManager(
            ILogger<STTProviderManager> logger,
            STTProviderRepository sttProviderRepository,
            IntegrationsManager integrationsManager)
        {
            _logger = logger;
            _sttProviderRepository = sttProviderRepository;
            _integrationsManager = integrationsManager;

            InitializeProvidersAsync().GetAwaiter().GetResult();
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
                    var addResult = await AddProvider(providerEnum);
                    if (!addResult.Success)
                    {
                        throw new Exception($"Failed to add stt provider: {providerEnum}: [{addResult.Code}] {addResult.Message}");
                    }
                }

                RegisterProviderService(providerEnum);
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
                    var returnedProviderEnum = (InterfaceSTTProviderEnum)getProviderTypeMethod.Invoke(null, null)!;
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

            try
            {
                var providerList = await _sttProviderRepository.GetProviderListAsync(page, pageSize);
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

        private async Task<FunctionReturnResult<STTProviderData>> AddProvider(InterfaceSTTProviderEnum providerId)
        {
            var result = new FunctionReturnResult<STTProviderData>();

            try
            {
                var providerData = new STTProviderData()
                {
                    Id = providerId,
                    DisabledAt = DateTime.UtcNow
                };

                if (providerData.Id == InterfaceSTTProviderEnum.Unknown)
                {
                    return result.SetFailureResult(
                        "AddProvider:INVALID_ID",
                        "Invalid provider ID"
                    );
                }

                var existingProvider = await _sttProviderRepository.GetProviderAsync(providerData.Id);
                if (existingProvider != null)
                {
                    return result.SetFailureResult(
                        "AddProvider:EXISTS",
                        "Provider already exists"
                    );
                }

                var addResult = await _sttProviderRepository.AddProviderAsync(providerData);
                if (!addResult)
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

        public Type? GetProviderService(InterfaceSTTProviderEnum providerId)
        {
            return _sttProviderClasses.TryGetValue(providerId, out var service) ? service : null;
        }

        public async Task<STTProviderData?> GetProviderData(InterfaceSTTProviderEnum providerId)
        {
            return await _sttProviderRepository.GetProviderAsync(providerId);
        }

        public async Task<FunctionReturnResult<STTProviderData?>> GetProviderDataByIntegration(string integrationType)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

            try
            {
                var providerData = await _sttProviderRepository.GetProviderDataByIntegration(integrationType);
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

        public async Task<FunctionReturnResult<STTProviderData?>> UpdateProvider(
            STTProviderData provider,
            IFormCollection formData,
            IntegrationsManager integrationsManager)
        {
            var result = new FunctionReturnResult<STTProviderData?>();

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
                var newProviderData = new STTProviderData
                {
                    Id = provider.Id,
                    Models = provider.Models
                };

                // Handle disabled state
                if (!root.TryGetProperty("disabled", out var disabledElement))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:DISABLED_STATE_NOT_FOUND",
                        "Provider disabled state not found"
                    );
                }

                bool disabled = disabledElement.GetBoolean();
                newProviderData.DisabledAt = disabled ? (provider.DisabledAt ?? DateTime.UtcNow) : null;

                // Handle integration selection
                if (!root.TryGetProperty("integrationId", out var integrationIdElement))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:INTEGRATION_ID_NOT_FOUND",
                        "Integration ID is missing"
                    );
                }

                string? integrationId = integrationIdElement.GetString();
                if (string.IsNullOrEmpty(integrationId))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:MISSING_INTEGRATION_ID", 
                        "Integration ID is required"
                    );
                }

                // Validate integration exists and is STT type
                var integration = await integrationsManager.getIntegrationData(integrationId);
                if (integration.Data == null || !integration.Success)
                {
                    return result.SetFailureResult(
                        "UpdateProvider:SELECTED_INTEGRATION_NOT_FOUND", 
                        "Selected integration not found"
                    );
                }

                if (!integration.Data.Type.Contains("STT") && !integration.Data.Type.Contains("SPEECH2TEXT"))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:INVALID_INTEGRATION", 
                        "Selected integration is not an STT integration"
                    );
                }

                newProviderData.IntegrationId = integrationId;

                // Handle integration fields
                if (!root.TryGetProperty("userIntegrationFields", out var fieldsElement))
                {
                    return result.SetFailureResult(
                        "UpdateProvider:USER_INTEGRATION_FIELDS_NOT_FOUND",
                        "User integration fields not found"
                    );
                }
                else
                {
                    var availableModelIds = newProviderData.Models.Select(m => m.Id).ToList();

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

                // Save to database
                var updateResult = await _sttProviderRepository.UpdateProviderAsync(newProviderData);
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
                    $"Failed to update provider: {ex.Message}"
                );
            }
        }

        public async Task<FunctionReturnResult<STTProviderModelData?>> AddUpdateProviderModel(
            STTProviderData provider,
            string modelId,
            string postType,
            STTProviderModelData? oldModelData,
            IFormCollection formData)
        {
            var result = new FunctionReturnResult<STTProviderModelData?>();

            try
            {
                var newModelData = new STTProviderModelData()
                {
                    Id = modelId
                };

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

                // Saving new data to database
                if (postType == "new")
                {
                    var addResult = await _sttProviderRepository.AddModelAsync(provider.Id, newModelData);
                    if (!addResult.IsAcknowledged || addResult.ModifiedCount == 0)
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:DB_ADD_FAILED", 
                            "Failed to add model to database"
                        );
                    }
                }
                else if (postType == "edit")
                {
                    var editResult = await _sttProviderRepository.UpdateModelAsync(provider.Id, newModelData);
                    if (!editResult.IsAcknowledged || editResult.ModifiedCount == 0)
                    {
                        return result.SetFailureResult(
                            "AddUpdateProviderModel:DB_UPDATE_FAILED",
                            "Failed to update model in database"
                        );
                    }
                }

                return result.SetSuccessResult(newModelData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddUpdateProviderModel:EXCEPTION",
                    ex.Message
                );
            }
        }

        public async Task<FunctionReturnResult<ISTTService?>> BuildProviderServiceByIntegration(
            BusinessAppIntegration integrationData,
            BusinessAppAgentIntegrationData agentIntegrationData,
            int inputSampleRate,
            int inputBitsPerSample,
            AudioEncodingTypeEnum inputAudioEncoding
        )
        {
            var result = new FunctionReturnResult<ISTTService?>();

            try
            {
                var sttProviderData = await GetProviderDataByIntegration(integrationData.Type);
                if (!sttProviderData.Success)
                {
                    return result.SetFailureResult(
                        "BuildProviderServiceByIntegration:PROVIDER_NOT_FOUND", 
                        "Provider not found by integration type"
                    );
                }

                // Create the Audio Detail Object used by the refactored services
                var ttsAudioFormat = new TTSProviderAvailableAudioFormat
                {
                    SampleRateHz = inputSampleRate,
                    BitsPerSample = inputBitsPerSample,
                    Encoding = inputAudioEncoding
                };

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

                switch (sttProviderData.Data!.Id)
                {
                    case InterfaceSTTProviderEnum.AzureSpeechServices:
                        {
                            string tenantId = integrationData.Fields["tenant_id"];
                            string clientId = integrationData.Fields["client_id"];
                            string clientSecret = _integrationsManager.DecryptField(integrationData.EncryptedFields["client_secret"]);
                            string subscriptionId = integrationData.Fields["subscription_id"];
                            string resourceGroupName = integrationData.Fields["resource_group_name"];
                            string speechResourceName = integrationData.Fields["speech_resource_name"];
                            string resourceRegion = integrationData.Fields["resource_region"];

                            var config = new AzureSpeechSTTConfig()
                            {
                                Language = GetString("langauge_id")!,
                                ContinuousLanguageIdentificationIds = GetList("continous_language_identification_ids"),
                                SpeakerDiarization = GetBool("speaker_diarization", false),
                                PhrasesList = GetList("phrases_list"),
                                SilenceTimeout = (int)GetInt("silence_timeout")!,
                            };

                            var azureSTTService = new AzureSpeechSTTService(
                                tenantId,
                                clientId,
                                clientSecret,
                                subscriptionId,
                                resourceGroupName,
                                speechResourceName,
                                resourceRegion,
                                config,
                                ttsAudioFormat
                            );

                            return result.SetSuccessResult(azureSTTService);
                        }

                    case InterfaceSTTProviderEnum.Deepgram:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            DeepgramSTTConfig config = new DeepgramSTTConfig()
                            {
                                Model = GetString("model")!,
                                Language = GetString("language")!,
                                KeywordsList = GetList("keywords_list"),
                                SilenceTimeout = (int)GetInt("silence_timeout")!,
                                SpeakerDiarization = GetBool("speaker_diarization", false),
                                Punctuate = GetBool("punctuate", true),
                                SmartFormat = GetBool("smart_format", false),
                                FillerWords = GetBool("filler_words", true),
                                ProfanityFilter = GetBool("profanity_filter", false),
                                FluxEotThreshold = GetDouble("flux_eot_threshold")
                            };

                            var deepgramSTTService = new DeepgramSTTService(
                                apiKey,
                                config,
                                ttsAudioFormat
                            );

                            return result.SetSuccessResult(deepgramSTTService);
                        }

                    case InterfaceSTTProviderEnum.AssemblyAI:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            AssemblyAISpeechSTTConfig config = new AssemblyAISpeechSTTConfig()
                            {
                                SpeechModel = GetString("speech_model")!,
                                FormatTurns = GetBool("format_turns", false),
                                EndOfTurnConfidenceThreshold = GetFloat("end_of_turn_confidence_threshold"),
                                MinEndOfTurnSilenceWhenConfident = GetInt("min_end_of_turn_silence_when_confident"),
                                MaxTurnSilence = GetInt("max_turn_silence"),
                                VADThreshold = GetDouble("vad_threshold"),
                                KeytermsPrompt = GetList("keyterms_prompt")
                            };

                            var assemblySTTService = new AssemblyAISpeechSTTService(
                                apiKey,
                                config,
                                ttsAudioFormat
                            );

                            return result.SetSuccessResult(assemblySTTService);
                        }

                    case InterfaceSTTProviderEnum.ElevenLabs:
                        {
                            string apiKey = _integrationsManager.DecryptField(integrationData.EncryptedFields["api_key"]);

                            ElevenLabsSTTConfig config = new ElevenLabsSTTConfig()
                            {
                                ModelId = GetString("model_id")!,
                                LanguageCode = GetString("language_code", ""),
                                VADSilenceThresholdSeconds = GetDouble("vad_silence_threshold"),
                                VADThreshold = GetDouble("vad_threshold"),
                                MinSpeechDurationMS = GetInt("min_speech_duration_ms"),
                                MinSilenceDurationMS = GetInt("min_silence_duration_ms")
                            };

                            var elevenLabsService = new ElevenLabsSTTService(
                                apiKey,
                                config,
                                ttsAudioFormat
                            );

                            return result.SetSuccessResult(elevenLabsService);
                        }

                    default:
                        _logger.LogError("Business app STT provider {ProviderType} not supported", sttProviderData.Data.Id);
                        return result.SetFailureResult(
                            "BuildProviderServiceByIntegration:NOT_SUPPORTED",
                            $"Provider {sttProviderData.Data.Id} not supported"
                        );
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
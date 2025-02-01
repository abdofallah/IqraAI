using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.LLM;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Services.LLM;
using IqraInfrastructure.Services.STT;
using IqraInfrastructure.Services.TTS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using System.Text.Json;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessAgentsManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAgentAudioRepository _businessAgentAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        public BusinessAgentsManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository, BusinessAgentAudioRepository businessAgentAudioRepository, AudioFileProcessor audioProcessor)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;

            _audioProcessor = audioProcessor;
        }

        public async Task<FunctionReturnResult<BusinessAppAgent?>> AddOrUpdateAgent(long businessId, string postType, IFormCollection formData, BusinessAppAgent? existingAgentData, LLMProviderManager llmProviderManager, STTProviderManager sttProviderManager, TTSProviderManager ttsProviderManager)
        {
            var result = new FunctionReturnResult<BusinessAppAgent?>();

            // Get business languages
            var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            // Parse changes data
            formData.TryGetValue("changes", out StringValues changesJsonString);
            if (string.IsNullOrWhiteSpace(changesJsonString))
            {
                result.Code = "AddOrUpdateAgent:2";
                result.Message = "Changes data is required.";
                return result;
            }

            JsonElement changesRootElement;
            try
            {
                changesRootElement = JsonSerializer.Deserialize<JsonElement>(changesJsonString.ToString());
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateAgent:3";
                result.Message = "Invalid changes data format: " + ex.Message;
                return result;
            }

            // Create new agent instance
            var newAgentData = new BusinessAppAgent();

            // General Section
            if (!changesRootElement.TryGetProperty("general", out var generalTabElement))
            {
                result.Code = "AddOrUpdateAgent:4";
                result.Message = "General section not found.";
                return result;
            }
            else
            {
                if (generalTabElement.TryGetProperty("emoji", out var emojiElement))
                {
                    newAgentData.General.Emoji = emojiElement.GetString();
                }

                var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "name",
                    newAgentData.General.Name
                );
                if (!nameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + nameValidationResult.Code;
                    result.Message = nameValidationResult.Message;
                    return result;
                }

                var descriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "description",
                    newAgentData.General.Description
                );
                if (!descriptionValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + descriptionValidationResult.Code;
                    result.Message = descriptionValidationResult.Message;
                    return result;
                }
            }

            // Context Section
            if (!changesRootElement.TryGetProperty("context", out var contextTabElement))
            {
                result.Code = "AddOrUpdateAgent:5";
                result.Message = "Context section not found.";
                return result;
            }
            else
            {
                if (contextTabElement.TryGetProperty("useBranding", out var useBrandingElement))
                {
                    newAgentData.Context.UseBranding = useBrandingElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useBranches", out var useBranchesElement))
                {
                    newAgentData.Context.UseBranches = useBranchesElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useServices", out var useServicesElement))
                {
                    newAgentData.Context.UseServices = useServicesElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useProducts", out var useProductsElement))
                {
                    newAgentData.Context.UseProducts = useProductsElement.GetBoolean();
                }
            }

            // Personality Section
            if (!changesRootElement.TryGetProperty("personality", out var personalityTabElement))
            {
                result.Code = "AddOrUpdateAgent:6";
                result.Message = "Personality section not found.";
                return result;
            }
            else
            {
                var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    personalityTabElement,
                    "name",
                    newAgentData.Personality.Name
                );
                if (!nameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + nameValidationResult.Code;
                    result.Message = nameValidationResult.Message;
                    return result;
                }

                var roleValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    personalityTabElement,
                    "role",
                    newAgentData.Personality.Role
                );
                if (!roleValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + roleValidationResult.Code;
                    result.Message = roleValidationResult.Message;
                    return result;
                }

                var capabilitiesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                    businessLanguages,
                    personalityTabElement,
                    "capabilities",
                    newAgentData.Personality.Capabilities,
                    true
                );
                if (!capabilitiesValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + capabilitiesValidationResult.Code;
                    result.Message = capabilitiesValidationResult.Message;
                    return result;
                }

                var ethicsValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                    businessLanguages,
                    personalityTabElement,
                    "ethics",
                    newAgentData.Personality.Ethics,
                    true
                );
                if (!ethicsValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + ethicsValidationResult.Code;
                    result.Message = ethicsValidationResult.Message;
                    return result;
                }

                var toneValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                    businessLanguages,
                    personalityTabElement,
                    "tone",
                    newAgentData.Personality.Tone,
                    true
                );
                if (!toneValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + toneValidationResult.Code;
                    result.Message = toneValidationResult.Message;
                    return result;
                }
            }

            // Utterances Section
            if (!changesRootElement.TryGetProperty("utterances", out var utterancesTabElement))
            {
                result.Code = "AddOrUpdateAgent:7";
                result.Message = "Utterances section not found.";
                return result;
            }
            else
            {
                if (!utterancesTabElement.TryGetProperty("openingType", out var openingTypeElement))
                {
                    result.Code = "AddOrUpdateAgent:8";
                    result.Message = "Missing opening type value.";
                    return result;
                }

                if (!openingTypeElement.TryGetInt32(out var openingTypeInt))
                {
                    result.Code = "AddOrUpdateAgent:9";
                    result.Message = "Invalid opening type value.";
                    return result;
                }

                if (!Enum.IsDefined(typeof(BusinessAppAgentOpeningType), openingTypeInt))
                {
                    result.Code = "AddOrUpdateAgent:10";
                    result.Message = "Opening type not found.";
                    return result;
                }

                newAgentData.Utterances.OpeningType = (BusinessAppAgentOpeningType)openingTypeInt;

                var greetingMessageValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    utterancesTabElement,
                    "greetingMessage",
                    newAgentData.Utterances.GreetingMessage
                );
                if (!greetingMessageValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + greetingMessageValidationResult.Code;
                    result.Message = greetingMessageValidationResult.Message;
                    return result;
                }

                var phrasesBeforeReplyValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    utterancesTabElement,
                    "phrasesBeforeReply",
                    newAgentData.Utterances.PhrasesBeforeReply,
                    true
                );
                if (!phrasesBeforeReplyValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgent:" + phrasesBeforeReplyValidationResult.Code;
                    result.Message = phrasesBeforeReplyValidationResult.Message;
                    return result;
                }
            }

            // Integrations Section
            if (!changesRootElement.TryGetProperty("integrations", out var integrationsTabElement))
            {
                result.Code = "AddOrUpdateAgent:11";
                result.Message = "Integrations section not found.";
                return result;
            }
            else
            {
                // STT Integration
                if (integrationsTabElement.TryGetProperty("STT", out var sttElement))
                {
                    foreach (var businessLanguage in businessLanguages)
                    {
                        newAgentData.Integrations.STT.Add(businessLanguage, new List<BusinessAppAgentIntegrationData>());

                        if (!sttElement.TryGetProperty(businessLanguage, out var sttLanguageElement))
                        {
                            result.Code = "AddOrUpdateAgent:12";
                            result.Message = "STT section for language " + businessLanguage + " not found. At least one integration is required.";
                            return result;
                        }

                        var validationResult = await ValidateIntegrationData(
                            businessId,
                            sttLanguageElement,
                            "STT",
                            businessLanguage,
                            _parentBusinessManager.GetIntegrationsManager(),
                            sttProviderManager
                        );

                        if (!validationResult.Success)
                        {
                            result.Code = "AddOrUpdateAgent:" + validationResult.Code;
                            result.Message = validationResult.Message;
                            return result;
                        }

                        newAgentData.Integrations.STT[businessLanguage] = validationResult.Data;
                    }
                }

                // LLM Integration
                if (integrationsTabElement.TryGetProperty("LLM", out var llmElement))
                {
                    foreach (var businessLanguage in businessLanguages)
                    {
                        newAgentData.Integrations.LLM.Add(businessLanguage, new List<BusinessAppAgentIntegrationData>());

                        if (!llmElement.TryGetProperty(businessLanguage, out var llmLanguageElement))
                        {
                            result.Code = "AddOrUpdateAgent:13";
                            result.Message = "LLM section for language " + businessLanguage + " not found. At least one integration is required.";
                            return result;
                        }

                        var validationResult = await ValidateIntegrationData(
                            businessId,
                            llmLanguageElement,
                            "LLM",
                            businessLanguage,
                            _parentBusinessManager.GetIntegrationsManager(),
                            llmProviderManager
                        );

                        if (!validationResult.Success)
                        {
                            result.Code = "AddOrUpdateAgent:" + validationResult.Code;
                            result.Message = validationResult.Message;
                            return result;
                        }

                        newAgentData.Integrations.LLM[businessLanguage] = validationResult.Data;
                    }
                }

                // TTS Integration
                if (integrationsTabElement.TryGetProperty("TTS", out var ttsElement))
                {
                    foreach (var businessLanguage in businessLanguages)
                    {
                        newAgentData.Integrations.TTS.Add(businessLanguage, new List<BusinessAppAgentIntegrationData>());

                        if (!ttsElement.TryGetProperty(businessLanguage, out var ttsLanguageElement))
                        {
                            result.Code = "AddOrUpdateAgent:14";
                            result.Message = "TTS section for language " + businessLanguage + " not found. At least one integration is required.";
                            return result;
                        }

                        var validationResult = await ValidateIntegrationData(
                            businessId,
                            ttsLanguageElement,
                            "TTS",
                            businessLanguage,
                            _parentBusinessManager.GetIntegrationsManager(),
                            ttsProviderManager
                        );

                        if (!validationResult.Success)
                        {
                            result.Code = "AddOrUpdateAgent:" + validationResult.Code;
                            result.Message = validationResult.Message;
                            return result;
                        }

                        newAgentData.Integrations.TTS[businessLanguage] = validationResult.Data;
                    }
                }
            }

            // Cache Section
            if (!changesRootElement.TryGetProperty("cache", out var cacheTabElement))
            {
                result.Code = "AddOrUpdateAgent:15";
                result.Message = "Cache section not found.";
                return result;
            }
            else
            {
                if (!cacheTabElement.TryGetProperty("messages", out var messagesElement))
                {
                    result.Code = "AddOrUpdateAgent:16";
                    result.Message = "Cache message groups section not found.";
                    return result;
                }
                else
                {
                    foreach (var messageCacheIdElement in messagesElement.EnumerateArray())
                    {
                        if (messageCacheIdElement.ValueKind != JsonValueKind.String)
                        {
                            result.Code = "AddOrUpdateAgent:17";
                            result.Message = $"Invalid array item type for cache message groups. Found: {messageCacheIdElement.ValueKind}";
                            return result;
                        }

                        var messagesCacheGroupId = messageCacheIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(messagesCacheGroupId))
                        {
                            result.Code = "AddOrUpdateAgent:18";
                            result.Message = "Empty array item type for cache message groups.";
                            return result;
                        }

                        var checkMessageCacheGroupExistsResult = await _parentBusinessManager.GetCacheManager().CheckBusinessCacheMessageGroupExists(businessId, messagesCacheGroupId);
                        if (!checkMessageCacheGroupExistsResult)
                        {
                            result.Code = "AddOrUpdateAgent:19";
                            result.Message = $"Cache message group does not exist with id: {messagesCacheGroupId}";
                            return result;
                        }

                        newAgentData.Cache.Messages.Add(messagesCacheGroupId);
                    }
                }
                

                if (!cacheTabElement.TryGetProperty("audios", out var audiosElement))
                {
                    result.Code = "AddOrUpdateAgent:20";
                    result.Message = "Cache audios section not found.";
                    return result;
                }
                else
                {
                    foreach (var aduioCacheIdElement in audiosElement.EnumerateArray())
                    {
                        if (aduioCacheIdElement.ValueKind != JsonValueKind.String)
                        {
                            result.Code = "AddOrUpdateAgent:21";
                            result.Message = $"Invalid array item type for cache audio groups. Found: {aduioCacheIdElement.ValueKind}";
                            return result;
                        }

                        var audiosCacheGroupId = aduioCacheIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(audiosCacheGroupId))
                        {
                            result.Code = "AddOrUpdateAgent:22";
                            result.Message = "Empty array item type for cache audio groups.";
                            return result;
                        }

                        var checkAudioCacheGroupExistsResult = await _parentBusinessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, audiosCacheGroupId);
                        if (!checkAudioCacheGroupExistsResult)
                        {
                            result.Code = "AddOrUpdateAgent:23";
                            result.Message = $"Cache audio group does not exist with id: {audiosCacheGroupId}";
                            return result;
                        }

                        newAgentData.Cache.Audios.Add(audiosCacheGroupId);
                    }
                }

                if (cacheTabElement.TryGetProperty("autoCacheAudioSettings", out var autoCacheSettingsElement))
                {
                    // TODO agent.Cache.AutoCacheAudioSettings
                    //newAgentData.Cache.AutoCacheAudioSettings = null;
                }
            }

            // Settings
            if (!changesRootElement.TryGetProperty("settings", out var settingsTabElement))
            {
                result.Code = "AddOrUpdateAgent:24";
                result.Message = "Cache section not found.";
                return result;
            }
            else
            {
                if (!settingsTabElement.TryGetProperty("backgroundAudioUrl", out var backgroundAudioUrlElement))
                {
                    result.Code = "AddOrUpdateAgent:25";
                    result.Message = "Background audio url not found.";
                    return result;
                }

                var backgroundAudioUrl = backgroundAudioUrlElement.GetString();
                if (!string.IsNullOrWhiteSpace(backgroundAudioUrl))
                {
                    if (backgroundAudioUrl == "custom")
                    {
                        var backgroundAudio = formData.Files.GetFile("backgroundAudio");
                        if (backgroundAudio == null)
                        {
                            result.Code = "AddOrUpdateAgent:26";
                            result.Message = "Background audio file not found.";
                            return result;
                        }

                        var validationResult = await _audioProcessor.ValidateAudioFile(backgroundAudio);
                        if (!validationResult.IsValid)
                        {
                            result.Code = "AddOrUpdateAgent:27";
                            result.Message = $"Background audio validation failed: {validationResult.ErrorMessage}.";
                            return result;
                        }

                        bool fileExists = await _businessAgentAudioRepository.FileExists(validationResult.Hash);
                        if (!fileExists)
                        {
                            var metadata = new Dictionary<string, string>
                                {
                                    { "fileContentType", validationResult.ContentType }
                                };

                            await _businessAgentAudioRepository.PutFileAsByteData(
                                validationResult.Hash,
                                validationResult.FileBytes,
                                metadata
                            );
                        }

                        newAgentData.Settings.BackgroundAudioUrl = validationResult.Hash;
                    }
                    else
                    {
                        newAgentData.Settings.BackgroundAudioUrl = backgroundAudioUrl;
                    }

                    if (!settingsTabElement.TryGetProperty("backgroundAudioVolume", out var backgroundAudioVolumeElement))
                    {
                        result.Code = "AddOrUpdateAgent:28";
                        result.Message = "Background audio volume not found.";
                        return result;
                    }

                    if (!backgroundAudioVolumeElement.TryGetInt32(out var backgroundAudioVolumeInt))
                    {
                        result.Code = "AddOrUpdateAgent:29";
                        result.Message = "Invalid background audio volume value.";
                        return result;
                    }

                    newAgentData.Settings.BackgroundAudioVolume = backgroundAudioVolumeInt;
                }
            }

            if (postType == "new")
            {
                newAgentData.Id = Guid.NewGuid().ToString();

                var addAgentResult = await _businessAppRepository.AddAgent(businessId, newAgentData);
                if (!addAgentResult)
                {
                    result.Code = "AddOrUpdateAgent:20";
                    result.Message = "Failed to add business agent.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                newAgentData.Id = existingAgentData.Id;
                newAgentData.Scripts = existingAgentData.Scripts;

                var updateAgentResult = await _businessAppRepository.UpdateAgent(businessId, newAgentData);
                if (!updateAgentResult)
                {
                    result.Code = "AddOrUpdateAgent:21";
                    result.Message = "Failed to update business agent.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = newAgentData;
            return result;
        }

        public async Task<bool> CheckAgentExists(long businessId, string agentId)
        {
            var result = await _businessAppRepository.CheckAgentExists(businessId, agentId);
            return result;
        }

        public async Task<BusinessAppAgent?> GetAgentById(long businessId, string agentId)
        {
            var result = await _businessAppRepository.GetAgentById(businessId, agentId);
            return result;
        }

        private async Task<FunctionReturnResult<List<BusinessAppAgentIntegrationData>>> ValidateIntegrationData(
            long businessId,
            JsonElement integrationElement,
            string integrationType,
            string businessLanguage,
            BusinessIntegrationsManager integrationsManager,
            dynamic providerManager)
        {
            var result = new FunctionReturnResult<List<BusinessAppAgentIntegrationData>>();
            var integrationList = new List<BusinessAppAgentIntegrationData>();

            var integrationElementArray = integrationElement.EnumerateArray();
            for (int i = 0; i < integrationElementArray.Count(); i++)
            {
                var currentIntegrationElement = integrationElementArray.ElementAt(i);

                if (!currentIntegrationElement.TryGetProperty("id", out var integrationIdElement))
                {
                    result.Code = "ValidateIntegrationData:1";
                    result.Message = $"{integrationType} integration id not found at index {i}.";
                    return result;
                }

                var integrationId = integrationIdElement.GetString();
                if (string.IsNullOrWhiteSpace(integrationId))
                {
                    result.Code = "ValidateIntegrationData:2";
                    result.Message = $"{integrationType} integration id is empty at index {i}.";
                    return result;
                }

                var currentIntegrationResult = await integrationsManager.getBusinessIntegrationById(businessId, integrationId);
                if (!currentIntegrationResult.Success)
                {
                    result.Code = "ValidateIntegrationData:" + currentIntegrationResult.Code;
                    result.Message = currentIntegrationResult.Message;
                    return result;
                }

                var providerData = await providerManager.GetProviderDataByIntegration(currentIntegrationResult.Data.Type);
                if (!providerData.Success)
                {
                    result.Code = "ValidateIntegrationData:" + providerData.Code;
                    result.Message = providerData.Message;
                    return result;
                }

                var newIntegrationData = new BusinessAppAgentIntegrationData()
                {
                    Id = integrationId,
                };

                if (!currentIntegrationElement.TryGetProperty("fieldValues", out var fieldValuesElement))
                {
                    result.Code = "ValidateIntegrationData:3";
                    result.Message = $"{integrationType} field values not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                    return result;
                }

                IEnumerable<ProviderFieldBase> userIntegrationFields;
                IEnumerable<ProviderModelBase> models;

                if (integrationType == "STT")
                {
                    var sttData = providerData.Data as STTProviderData;
                    userIntegrationFields = sttData.UserIntegrationFields;
                    models = sttData.Models;
                }
                else if (integrationType == "TTS")
                {
                    var ttsData = providerData.Data as TTSProviderData;
                    userIntegrationFields = ttsData.UserIntegrationFields;
                    models = ttsData.Models.Cast<TTSProviderSpeakerData>();
                }
                else if (integrationType == "LLM")
                {
                    var llmData = providerData.Data as LLMProviderData;
                    userIntegrationFields = llmData.UserIntegrationFields;
                    models = llmData.Models;
                }
                else
                {
                    result.Code = "ValidateIntegrationData:5";
                    result.Message = $"Unknown integration type: {integrationType}.";
                    return result;
                }

                if (userIntegrationFields == null || models == null)
                {
                    result.Code = "ValidateIntegrationData:6";
                    result.Message = $"Invalid provider data structure for {integrationType}.";
                    return result;
                }

                foreach (var integrationField in userIntegrationFields)
                {
                    bool fieldValueExists = fieldValuesElement.TryGetProperty(integrationField.Id, out var fieldValueElement);
                    if (integrationField.Required && !fieldValueExists)
                    {
                        result.Code = "ValidateIntegrationData:5";
                        result.Message = $"{integrationType} field value for field {integrationField.Name} not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                        return result;
                    }

                    if (integrationField.IsEncrypted)
                    {
                        result.Code = "ValidateIntegrationData:6";
                        result.Message = $"Encrypted {integrationType} field value for field {integrationField.Name} is not supported in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                        return result;
                    }

                    if (fieldValueExists)
                    {
                        switch (integrationField.Type)
                        {
                            case "string":
                                var fieldValueString = fieldValueElement.GetString();
                                if (string.IsNullOrWhiteSpace(fieldValueString))
                                {
                                    result.Code = "ValidateIntegrationData:7";
                                    result.Message = $"{integrationType} string value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }
                                newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueString);
                                break;

                            case "select":
                            case "models":
                                var fieldValueOptionKey = fieldValueElement.GetString();
                                if (string.IsNullOrWhiteSpace(fieldValueOptionKey))
                                {
                                    result.Code = "ValidateIntegrationData:8";
                                    result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }

                                if (integrationField.Type == "select")
                                {
                                    if (integrationField.Options == null || integrationField.Options.Find(d => d.Key == fieldValueOptionKey) == null)
                                    {
                                        result.Code = "ValidateIntegrationData:9";
                                        result.Message = $"{integrationType} option value for select field {integrationField.Name} not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                        return result;
                                    }
                                }

                                if (integrationField.Type == "models")
                                {
                                    var fieldValueModelData = models.ToList().Find(x => x.Id == fieldValueOptionKey);
                                    if (fieldValueModelData == null)
                                    {
                                        result.Code = "ValidateIntegrationData:10";
                                        result.Message = $"{integrationType} model is not found in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                        return result;
                                    }

                                    if (fieldValueModelData.DisabledAt != null)
                                    {
                                        result.Code = "ValidateIntegrationData:11";
                                        result.Message = $"{integrationType} model is disabled in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                        return result;
                                    }

                                    if (integrationType == "TTS")
                                    {
                                        var ttsSpeaker = fieldValueModelData as TTSProviderSpeakerData;
                                        if (ttsSpeaker == null)
                                        {
                                            result.Code = "ValidateIntegrationData:12";
                                            result.Message = $"Invalid TTS speaker data structure in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                            return result;
                                        }

                                        if (!ttsSpeaker.IsMultilingual && !ttsSpeaker.SupportedLanguages.Contains(businessLanguage))
                                        {
                                            result.Code = "ValidateIntegrationData:13";
                                            result.Message = $"TTS speaker '{ttsSpeaker.Name}' does not support language '{businessLanguage}' in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                            return result;
                                        }
                                    }
                                }

                                newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueOptionKey);
                                break;

                            case "number":
                                if (fieldValueElement.ValueKind == JsonValueKind.String)
                                {
                                    var fieldValueNumberString = fieldValueElement.GetString();
                                    if (string.IsNullOrWhiteSpace(fieldValueNumberString) || !int.TryParse(fieldValueNumberString, out var fieldValueNumber))
                                    {
                                        result.Code = "ValidateIntegrationData:14";
                                        result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                        return result;
                                    }

                                    newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueNumber);
                                }
                                else if (fieldValueElement.ValueKind == JsonValueKind.Number)
                                {
                                    newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueElement.GetInt32());
                                }
                                else
                                {
                                    result.Code = "ValidateIntegrationData:15";
                                    result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }
                                break;

                            case "double_number":
                                if (fieldValueElement.ValueKind == JsonValueKind.String)
                                {
                                    var fieldValueNumberString = fieldValueElement.GetString();
                                    if (string.IsNullOrWhiteSpace(fieldValueNumberString) || !double.TryParse(fieldValueNumberString, out var fieldValueNumber))
                                    {
                                        result.Code = "ValidateIntegrationData:16";
                                        result.Message = $"{integrationType} field value for field {integrationField.Name} is empty in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                        return result;
                                    }

                                    newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueNumber);
                                }
                                else if (fieldValueElement.ValueKind == JsonValueKind.Number)
                                {
                                    newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueElement.GetDouble());
                                }
                                else
                                {
                                    result.Code = "ValidateIntegrationData:17";
                                    result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }
                                break;

                            case "boolean":
                                if (fieldValueElement.ValueKind != JsonValueKind.True && fieldValueElement.ValueKind != JsonValueKind.False)
                                {
                                    result.Code = "ValidateIntegrationData:18";
                                    result.Message = $"Invalid {integrationType} field value for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                    return result;
                                }

                                bool fieldValueBooleanValid = fieldValueElement.GetBoolean();
                                newIntegrationData.FieldValues.Add(integrationField.Id, fieldValueBooleanValid);
                                break;

                            default:
                                result.Code = "ValidateIntegrationData:19";
                                result.Message = $"Invalid {integrationType} field type for field {integrationField.Name} in integration with name {currentIntegrationResult.Data.FriendlyName}.";
                                return result;
                        }
                    }
                }

                integrationList.Add(newIntegrationData);
            }

            result.Success = true;
            result.Data = integrationList;
            return result;
        }
    }
}

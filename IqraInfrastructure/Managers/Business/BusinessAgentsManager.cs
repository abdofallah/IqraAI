using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Agent.Script.Node.StartNode;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.LLM;
using IqraCore.Entities.ProviderBase;
using IqraCore.Entities.STT;
using IqraCore.Entities.TTS;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using IqraInfrastructure.Managers.LLM;
using IqraInfrastructure.Managers.STT;
using IqraInfrastructure.Managers.TTS;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
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

        // SAVING/ADDING AGENT
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
                if (integrationsTabElement.TryGetProperty("stt", out var sttElement))
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
                if (integrationsTabElement.TryGetProperty("llm", out var llmElement))
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
                if (integrationsTabElement.TryGetProperty("tts", out var ttsElement))
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
                    else if (backgroundAudioUrl == "previous")
                    {
                        if (string.IsNullOrWhiteSpace(existingAgentData.Settings.BackgroundAudioUrl))
                        {
                            result.Code = "AddOrUpdateAgent:28";
                            result.Message = "Previous background audio url not found.";
                            return result;
                        }
                        newAgentData.Settings.BackgroundAudioUrl = existingAgentData.Settings.BackgroundAudioUrl;
                    }
                    else
                    {
                        result.Code = "AddOrUpdateAgent:29";
                        result.Message = "Invalid background audio url type (allowed custom or previous).";
                        return result;
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

        // SAVING/ADDING SCRIPT
        public async Task<FunctionReturnResult<BusinessAppAgentScript?>> AddOrUpdateAgentScript(
            long businessId,
            string agentId,
            string postType,
            IFormCollection formData,
            BusinessAppAgentScript? existingScriptData
        )
        {
            var result = new FunctionReturnResult<BusinessAppAgentScript?>();

            // Get business languages
            var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

            // Parse changes data
            formData.TryGetValue("changes", out StringValues changesJsonString);
            if (string.IsNullOrWhiteSpace(changesJsonString))
            {
                result.Code = "AddOrUpdateAgentScript:1";
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
                result.Code = "AddOrUpdateAgentScript:2";
                result.Message = "Invalid changes data format: " + ex.Message;
                return result;
            }

            // Create new script instance
            var newScriptData = new BusinessAppAgentScript();

            // General Section
            if (!changesRootElement.TryGetProperty("general", out var generalTabElement))
            {
                result.Code = "AddOrUpdateAgentScript:3";
                result.Message = "General section not found.";
                return result;
            }
            else
            {
                var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "name",
                    newScriptData.General.Name
                );
                if (!nameValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgentScript:" + nameValidationResult.Code;
                    result.Message = nameValidationResult.Message;
                    return result;
                }

                var descriptionValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "description",
                    newScriptData.General.Description
                );
                if (!descriptionValidationResult.Success)
                {
                    result.Code = "AddOrUpdateAgentScript:" + descriptionValidationResult.Code;
                    result.Message = descriptionValidationResult.Message;
                    return result;
                }
            }

            // Nodes Section
            if (!changesRootElement.TryGetProperty("nodes", out var nodesElement))
            {
                result.Code = "AddOrUpdateAgentScript:4";
                result.Message = "Nodes section not found.";
                return result;
            }

            var validateNodesResult = await ValidateAndCreateNodes(businessId, agentId, existingScriptData?.Id, nodesElement, businessLanguages);
            if (!validateNodesResult.Success)
            {
                result.Code = "AddOrUpdateAgentScript:" + validateNodesResult.Code;
                result.Message = validateNodesResult.Message;
                return result;
            }

            newScriptData.Nodes = validateNodesResult.Data;

            // Edges Section
            if (!changesRootElement.TryGetProperty("edges", out var edgesElement))
            {
                result.Code = "AddOrUpdateAgentScript:5";
                result.Message = "Edges section not found.";
                return result;
            }

            var validateEdgesResult = ValidateAndCreateEdges(edgesElement, newScriptData.Nodes);
            if (!validateEdgesResult.Success)
            {
                result.Code = "AddOrUpdateAgentScript:" + validateEdgesResult.Code;
                result.Message = validateEdgesResult.Message;
                return result;
            }

            newScriptData.Edges = validateEdgesResult.Data;

            // Additional Validations
            if (newScriptData.Nodes.Count == 0)
            {
                result.Code = "AddOrUpdateAgentScript:6";
                result.Message = "Script must contain at least one node.";
                return result;
            }

            if (newScriptData.Edges.Count == 0)
            {
                result.Code = "AddOrUpdateAgentScript:7";
                result.Message = "Script must contain at least one connection.";
                return result;
            }

            try
            {
                if (postType == "new")
                {
                    newScriptData.Id = Guid.NewGuid().ToString();

                    var updateResult = await _businessAppRepository.AddAgentScript(businessId, agentId, newScriptData);
                    if (!updateResult)
                    {
                        result.Code = "AddOrUpdateAgentScript:8";
                        result.Message = "Failed to add new script to agent.";
                        return result;
                    }
                }
                else
                {
                    newScriptData.Id = existingScriptData.Id;

                    var updateResult = await _businessAppRepository.UpdateAgentScript(businessId, agentId, newScriptData);
                    if (!updateResult)
                    {
                        result.Code = "AddOrUpdateAgentScript:9";
                        result.Message = "Failed to update existing script.";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Code = "AddOrUpdateAgentScript:10";
                result.Message = "Error occurred while saving script: " + ex.Message;
                return result;
            }

            result.Success = true;
            result.Data = newScriptData;
            return result;
        }

        private async Task<FunctionReturnResult<List<BusinessAppAgentScriptNode>>> ValidateAndCreateNodes(
            long businessId,
            string agentId,
            string? existingScriptId,
            JsonElement nodesElement,
            IEnumerable<string> businessLanguages
        )
        {
            var result = new FunctionReturnResult<List<BusinessAppAgentScriptNode>>();
            var nodes = new List<BusinessAppAgentScriptNode>();

            bool hasStartNode = false;

            foreach (JsonElement nodeElement in nodesElement.EnumerateArray())
            {
                // Validate required properties
                if (!nodeElement.TryGetProperty("id", out var nodeIdElement))
                {
                    result.Code = "ValidateAndCreateNodes:1";
                    result.Message = "Node id not found.";
                    return result;
                }

                if (!nodeElement.TryGetProperty("type", out var nodeTypeElement))
                {
                    result.Code = "ValidateAndCreateNodes:2";
                    result.Message = "Node type not found.";
                    return result;
                }

                if (!nodeTypeElement.TryGetInt32(out var nodeTypeInt))
                {
                    result.Code = "ValidateAndCreateNodes:3";
                    result.Message = "Invalid node type.";
                    return result;
                }

                if (!Enum.IsDefined(typeof(BusinessAppAgentScriptNodeTypeENUM), nodeTypeInt))
                {
                    result.Code = "ValidateAndCreateNodes:4";
                    result.Message = "Invalid node type.";
                    return result;
                }

                if (!nodeElement.TryGetProperty("position", out var positionElement))
                {
                    result.Code = "ValidateAndCreateNodes:5";
                    result.Message = "Node position not found.";
                    return result;
                }

                if (!positionElement.TryGetProperty("x", out var positionXElement) ||
                    !positionElement.TryGetProperty("y", out var positionYElement))
                {
                    result.Code = "ValidateAndCreateNodes:5";
                    result.Message = "Invalid node position data.";
                    return result;
                }

                var nodeId = nodeIdElement.GetString();
                BusinessAppAgentScriptNodeTypeENUM nodeType = (BusinessAppAgentScriptNodeTypeENUM)nodeTypeInt;
                var position = new BusinessAppAgentScriptNodePosition
                {
                    X = positionXElement.GetDouble(),
                    Y = positionYElement.GetDouble()
                };

                // Handle different node types
                if (nodeType == BusinessAppAgentScriptNodeTypeENUM.Start)
                {
                    if (hasStartNode)
                    {
                        result.Code = "ValidateAndCreateNodes:5";
                        result.Message = "Multiple start nodes found.";
                        return result;
                    }

                    hasStartNode = true;
                    nodes.Add(new BusinessAppAgentScriptStartNode
                    {
                        Id = nodeId,
                        Position = position
                    });
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.UserQuery)
                {
                    if (!nodeElement.TryGetProperty("query", out var queryElement))
                    {
                        result.Code = "ValidateAndCreateNodes:6";
                        result.Message = "User query data not found.";
                        return result;
                    }

                    var userQueryNode = new BusinessAppAgentScriptUserQueryNode
                    {
                        Id = nodeId,
                        Position = position
                    };

                    var queryValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        nodeElement,
                        "query",
                        userQueryNode.Query
                    );
                    if (!queryValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + queryValidationResult.Code;
                        result.Message = queryValidationResult.Message;
                        return result;
                    }

                    var examplesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                        businessLanguages,
                        nodeElement,
                        "examples",
                        userQueryNode.Examples,
                        true
                    );
                    if (!examplesValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + examplesValidationResult.Code;
                        result.Message = examplesValidationResult.Message;
                        return result;
                    }

                    nodes.Add(userQueryNode);
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse)
                {
                    if (!nodeElement.TryGetProperty("response", out var responseElement))
                    {
                        result.Code = "ValidateAndCreateNodes:7";
                        result.Message = "AI response data not found.";
                        return result;
                    }

                    var aiResponseNode = new BusinessAppAgentScriptAIResponseNode
                    {
                        Id = nodeId,
                        Position = position
                    };

                    var responseValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                        businessLanguages,
                        nodeElement,
                        "response",
                        aiResponseNode.Response
                    );
                    if (!responseValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + responseValidationResult.Code;
                        result.Message = responseValidationResult.Message;
                        return result;
                    }

                    var examplesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageListProperty(
                        businessLanguages,
                        nodeElement,
                        "examples",
                        aiResponseNode.Examples,
                        true
                    );
                    if (!examplesValidationResult.Success)
                    {
                        result.Code = "ValidateAndCreateNodes:" + examplesValidationResult.Code;
                        result.Message = examplesValidationResult.Message;
                        return result;
                    }

                    nodes.Add(aiResponseNode);
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    if (!nodeElement.TryGetProperty("toolType", out var toolTypeElement))
                    {
                        result.Code = "ValidateAndCreateNodes:10";
                        result.Message = "System tool type not found.";
                        return result;
                    }

                    if (!toolTypeElement.TryGetInt32(out var toolTypeInt))
                    {
                        result.Code = "ValidateAndCreateNodes:11";
                        result.Message = "Invalid system tool type.";
                        return result;
                    }

                    if (!Enum.IsDefined(typeof(BusinessAppAgentScriptNodeSystemToolTypeENUM), toolTypeInt))
                    {
                        result.Code = "ValidateAndCreateNodes:12";
                        result.Message = "Invalid system tool type.";
                        return result;
                    }

                    BusinessAppAgentScriptNodeSystemToolTypeENUM toolType = (BusinessAppAgentScriptNodeSystemToolTypeENUM)toolTypeInt;

                    if (!nodeElement.TryGetProperty("config", out var toolConfigElement))
                    {
                        result.Code = "ValidateAndCreateNodes:12";
                        result.Message = "System tool config not found.";
                        return result;
                    }

                    // End Call Tool
                    if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.EndCall)
                    {
                        var endCallNode = new BusinessAppAgentScriptEndCallToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("type", out var endCallTypeElement))
                        {
                            result.Code = "ValidateAndCreateNodes:12";
                            result.Message = "End call type not found.";
                            return result;
                        }

                        if (!endCallTypeElement.TryGetInt32(out var endCallTypeInt))
                        {
                            result.Code = "ValidateAndCreateNodes:13";
                            result.Message = "Invalid end call type.";
                            return result;
                        }

                        if (!Enum.IsDefined(typeof(BusinessAppAgentScriptEndCallTypeENUM), endCallTypeInt))
                        {
                            result.Code = "ValidateAndCreateNodes:14";
                            result.Message = "Invalid end call type.";
                            return result;
                        }

                        endCallNode.Type = (BusinessAppAgentScriptEndCallTypeENUM)endCallTypeInt;
                        if (endCallNode.Type == BusinessAppAgentScriptEndCallTypeENUM.WithMessage)
                        {
                            endCallNode.Messages = new Dictionary<string, string>();

                            var messagesValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                                businessLanguages,
                                toolConfigElement,
                                "messages",
                                endCallNode.Messages
                            );
                            if (!messagesValidationResult.Success)
                            {
                                result.Code = "ValidateAndCreateNodes:" + messagesValidationResult.Code;
                                result.Message = messagesValidationResult.Message;
                                return result;
                            }
                        }

                        nodes.Add(endCallNode);
                    }
                    // DTMF Input Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput)
                    {
                        var dtmfNode = new BusinessAppAgentScriptDTMFInputToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("timeout", out var timeoutElement))
                        {
                            result.Code = "ValidateAndCreateNodes:15";
                            result.Message = "DTMF timeout not found.";
                            return result;
                        }
                        dtmfNode.Timeout = timeoutElement.GetInt32();

                        if (!toolConfigElement.TryGetProperty("requireStartAsterisk", out var requireStartElement))
                        {
                            result.Code = "ValidateAndCreateNodes:16";
                            result.Message = "DTMF require start asterisk not found.";
                            return result;
                        }
                        dtmfNode.RequireStartAsterisk = requireStartElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("requireEndHash", out var requireEndElement))
                        {
                            result.Code = "ValidateAndCreateNodes:17";
                            result.Message = "DTMF require end hash not found.";
                            return result;
                        }
                        dtmfNode.RequireEndHash = requireEndElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("maxLength", out var maxLengthElement))
                        {
                            result.Code = "ValidateAndCreateNodes:18";
                            result.Message = "DTMF max length not found.";
                            return result;
                        }
                        dtmfNode.MaxLength = maxLengthElement.GetInt32();

                        if (!toolConfigElement.TryGetProperty("encryptInput", out var encryptElement))
                        {
                            result.Code = "ValidateAndCreateNodes:19";
                            result.Message = "DTMF encrypt input not found.";
                            return result;
                        }
                        dtmfNode.EncryptInput = encryptElement.GetBoolean();

                        if (dtmfNode.EncryptInput)
                        {
                            if (!toolConfigElement.TryGetProperty("variableName", out var variableNameElement))
                            {
                                result.Code = "ValidateAndCreateNodes:20";
                                result.Message = "DTMF variable name not found.";
                                return result;
                            }
                            dtmfNode.VariableName = variableNameElement.GetString();
                        }

                        if (!toolConfigElement.TryGetProperty("outcomes", out var outcomesElement))
                        {
                            result.Code = "ValidateAndCreateNodes:21";
                            result.Message = "DTMF outcomes not found.";
                            return result;
                        }

                        foreach (var outcomeElement in outcomesElement.EnumerateArray())
                        {
                            var newOutcomeData = new BusinessAppAgentScriptDTMFOutcome();

                            var valueValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                                businessLanguages,
                                outcomeElement,
                                "value",
                                newOutcomeData.Value
                            );
                            if (!valueValidationResult.Success)
                            {
                                result.Code = "ValidateAndCreateNodes:" + valueValidationResult.Code;
                                result.Message = valueValidationResult.Message;
                                return result;
                            }

                            if (!outcomeElement.TryGetProperty("portId", out var portIdElement))
                            {
                                result.Code = "ValidateAndCreateNodes:22";
                                result.Message = "DTMF port ID not found.";
                                return result;
                            }

                            newOutcomeData.PortId = portIdElement.GetString();

                            dtmfNode.Outcomes.Add(newOutcomeData);
                        }

                        nodes.Add(dtmfNode);
                    }
                    // Transfer To Agent Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent)
                    {
                        var transferNode = new BusinessAppAgentScriptTransferToAgentToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("agentId", out var agentIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:23";
                            result.Message = "Transfer agent ID not found.";
                            return result;
                        }
                        var transferAgentId = agentIdElement.GetString();
                        if (!string.IsNullOrWhiteSpace(transferAgentId))
                        {
                            var transferAgent = await _parentBusinessManager.GetAgentsManager().GetAgentById(businessId, transferAgentId);
                            if (transferAgent == null)
                            {
                                result.Code = "ValidateAndCreateNodes:24";
                                result.Message = "Transfer agent not found.";
                                return result;
                            }
                            transferNode.AgentId = transferAgentId;
                        }

                        if (!toolConfigElement.TryGetProperty("transferContext", out var transferContextElement))
                        {
                            result.Code = "ValidateAndCreateNodes:25";
                            result.Message = "Transfer context flag not found.";
                            return result;
                        }
                        transferNode.TransferConversation = transferContextElement.GetBoolean();

                        if (!toolConfigElement.TryGetProperty("summarizeContext", out var summarizeContextElement))
                        {
                            result.Code = "ValidateAndCreateNodes:26";
                            result.Message = "Summarize context flag not found.";
                            return result;
                        }
                        transferNode.SummarizeConversation = summarizeContextElement.GetBoolean();

                        nodes.Add(transferNode);
                    }
                    // Add Script To Context Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext)
                    {
                        var addScriptNode = new BusinessAppAgentScriptAddScriptToContextToolNode
                        {
                            Id = nodeId,
                            Position = position
                        };

                        if (!toolConfigElement.TryGetProperty("scriptId", out var scriptIdElement))
                        {
                            result.Code = "ValidateAndCreateNodes:27";
                            result.Message = "Script ID not found.";
                            return result;
                        }

                        var scriptId = scriptIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(scriptId))
                        {
                            result.Code = "ValidateAndCreateNodes:28";
                            result.Message = "Script ID invalid for add script to context node.";
                            return result;
                        }

                        if (existingScriptId != null && !string.IsNullOrWhiteSpace(existingScriptId) && scriptId == existingScriptId)
                        {
                            result.Code = "ValidateAndCreateNodes:29";
                            result.Message = "Script ID can not point to current script for add script to context node.";
                            return result;
                        }

                        bool scriptExists = await _businessAppRepository.CheckAgentScriptExists(businessId, agentId, scriptId);
                        if (!scriptExists)
                        {
                            result.Code = "ValidateAndCreateNodes:29";
                            result.Message = "Script not found for add script to context node.";
                            return result;
                        }

                        addScriptNode.ScriptId = scriptId;

                        nodes.Add(addScriptNode);
                    }
                    // Change Language Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.ChangeLanguage)
                    {
                        var changeLanguageNode = new BusinessAppAgentScriptSystemToolNode()
                        {
                            Id = nodeId,
                            Position = position,
                            ToolType = BusinessAppAgentScriptNodeSystemToolTypeENUM.ChangeLanguage,
                        };

                        nodes.Add(changeLanguageNode);
                    }
                    // Press DTMF Keypad Tool
                    else if (toolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.PressDTMFKeypad)
                    {
                        var pressDtmfKeypadNode = new BusinessAppAgentScriptSystemToolNode()
                        {
                            Id = nodeId,
                            Position = position,
                            ToolType = BusinessAppAgentScriptNodeSystemToolTypeENUM.PressDTMFKeypad,
                        };
                        
                        nodes.Add(pressDtmfKeypadNode);
                    }
                    // Unknown System Tool
                    else 
                    {
                        result.Code = "ValidateAndCreateNodes:28";
                        result.Message = $"Unknown system tool type: {toolType}";
                        return result;
                    }
                }
                else if (nodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool)
                {
                    if (!nodeElement.TryGetProperty("toolId", out var toolIdElement))
                    {
                        result.Code = "ValidateAndCreateNodes:29";
                        result.Message = "Custom tool ID not found.";
                        return result;
                    }

                    var toolId = toolIdElement.GetString();
                    if (string.IsNullOrWhiteSpace(toolId))
                    {
                        result.Code = "ValidateAndCreateNodes:30";
                        result.Message = "Invalid custom tool ID.";
                        return result;
                    }

                    // Validate tool exists
                    var tool = await _parentBusinessManager.GetToolsManager().CheckBusinessToolExists(businessId, toolId);
                    if (!tool)
                    {
                        result.Code = "ValidateAndCreateNodes:31";
                        result.Message = "Custom tool not found.";
                        return result;
                    }

                    var customToolNode = new BusinessAppAgentScriptCustomToolNode
                    {
                        Id = nodeId,
                        Position = position,
                        ToolId = toolId
                    };

                    // Validate and assign tool configuration
                    if (!nodeElement.TryGetProperty("config", out var configElement))
                    {
                        result.Code = "ValidateAndCreateNodes:32";
                        result.Message = "Custom tool configuration not found.";
                        return result;
                    }

                    foreach (var configProperty in configElement.EnumerateObject())
                    {
                        customToolNode.ToolConfiguration[configProperty.Name] = configProperty.Value.GetString() ?? "";
                    }

                    nodes.Add(customToolNode);
                }
                else
                {
                    result.Code = "ValidateAndCreateNodes:35";
                    result.Message = $"Unknown node type: {nodeType}";
                    return result;
                }
            }

            // Final validations
            if (!hasStartNode)
            {
                result.Code = "ValidateAndCreateNodes:8";
                result.Message = "Start node is required.";
                return result;
            }

            result.Success = true;
            result.Data = nodes;
            return result;
        }

        private FunctionReturnResult<List<BusinessAppAgentScriptEdge>> ValidateAndCreateEdges(
            JsonElement edgesElement,
            List<BusinessAppAgentScriptNode> nodes)
        {
            var result = new FunctionReturnResult<List<BusinessAppAgentScriptEdge>>();
            var edges = new List<BusinessAppAgentScriptEdge>();

            foreach (JsonElement edgeElement in edgesElement.EnumerateArray())
            {
                // Validate required properties
                if (!edgeElement.TryGetProperty("id", out var edgeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:1";
                    result.Message = "Edge ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("sourceNodeId", out var sourceNodeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:2";
                    result.Message = "Source node ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("sourceNodePortId", out var sourcePortIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:3";
                    result.Message = "Source port ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("targetNodeId", out var targetNodeIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:4";
                    result.Message = "Target node ID not found.";
                    return result;
                }

                if (!edgeElement.TryGetProperty("targetNodePortId", out var targetPortIdElement))
                {
                    result.Code = "ValidateAndCreateEdges:5";
                    result.Message = "Target port ID not found.";
                    return result;
                }

                string? edgeId = edgeIdElement.GetString();
                string? sourceNodeId = sourceNodeIdElement.GetString();
                string? sourcePortId = sourcePortIdElement.GetString();
                string? targetNodeId = targetNodeIdElement.GetString();
                string? targetPortId = targetPortIdElement.GetString();

                if (string.IsNullOrWhiteSpace(edgeId) || string.IsNullOrWhiteSpace(sourceNodeId) || string.IsNullOrWhiteSpace(sourcePortId))
                {
                    result.Code = "ValidateAndCreateEdges:6";
                    result.Message = "Invalid edge data.";
                    return result;
                }

                // Validate source node exists
                var sourceNode = nodes.FirstOrDefault(n => n.Id == sourceNodeId);
                if (sourceNode == null)
                {
                    result.Code = "ValidateAndCreateEdges:7";
                    result.Message = $"Source node not found: {sourceNodeId}";
                    return result;
                }

                if (!string.IsNullOrEmpty(targetNodeId))
                {
                    // Validate target node exists
                    var targetNode = nodes.FirstOrDefault(n => n.Id == targetNodeId);
                    if (targetNode == null)
                    {
                        result.Code = "ValidateAndCreateEdges:8";
                        result.Message = $"Target node not found: {targetNodeId}";
                        return result;
                    }

                    // Validate connection rules
                    var connectionValidation = ValidateNodeConnection(sourceNode, targetNode, sourcePortId, targetPortId);
                    if (!connectionValidation.Success)
                    {
                        result.Code = "ValidateAndCreateEdges:" + connectionValidation.Code;
                        result.Message = connectionValidation.Message;
                        return result;
                    }
                }              

                // Create edge
                var edge = new BusinessAppAgentScriptEdge
                {
                    Id = edgeId,
                    SourceNodeId = sourceNodeId,
                    SourceNodePortId = sourcePortId,
                    TargetNodeId = targetNodeId ?? "",
                    TargetNodePortId = targetPortId ?? ""
                };

                edges.Add(edge);
            }

            // Validate start node is connected
            var startNode = nodes.FirstOrDefault(n => n.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start);
            if (startNode != null && !edges.Any(e => e.SourceNodeId == startNode.Id))
            {
                result.Code = "ValidateAndCreateEdges:8";
                result.Message = "Start node must be connected to at least one node.";
                return result;
            }

            result.Success = true;
            result.Data = edges;
            return result;
        }

        private FunctionReturnResult ValidateNodeConnection(
            BusinessAppAgentScriptNode sourceNode,
            BusinessAppAgentScriptNode targetNode,
            string? sourcePortId,
            string? targetPortId)
        {
            var result = new FunctionReturnResult();

            // Start node cannot connect to AI response node
            if (sourceNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.Start &&
                targetNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse)
            {
                result.Code = "1";
                result.Message = "Start node cannot connect to AI Response node.";
                return result;
            }

            // AI response node can only connect to user query node
            if (sourceNode.NodeType == BusinessAppAgentScriptNodeTypeENUM.AIResponse &&
                targetNode.NodeType != BusinessAppAgentScriptNodeTypeENUM.UserQuery)
            {
                result.Code = "2";
                result.Message = "AI Response node can only connect to User Query node.";
                return result;
            }

            result.Success = true;
            return result;
        }
    }
}

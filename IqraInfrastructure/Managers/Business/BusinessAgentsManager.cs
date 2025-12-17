using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
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
using IqraInfrastructure.Helpers.Business;
using IqraCore.Entities.S3Storage;
using IqraInfrastructure.Repositories.S3Storage;
using MongoDB.Bson;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessAgentsManager
    {
        private readonly IMongoClient _mongoClient;
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly S3StorageClientFactory _s3StorageClientFactory;
        private readonly BusinessAgentAudioRepository _businessAgentAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        public BusinessAgentsManager(
            BusinessManager businessManager,
            IMongoClient mongoClient,
            BusinessAppRepository businessAppRepository,
            BusinessRepository businessRepository,
            S3StorageClientFactory s3StorageClientFactory,
            BusinessAgentAudioRepository businessAgentAudioRepository,
            AudioFileProcessor audioProcessor,
            IntegrationConfigurationManager integrationConfigurationManager
        )
        {
            _mongoClient = mongoClient;
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;

            _businessAgentAudioRepository = businessAgentAudioRepository;
            _s3StorageClientFactory = s3StorageClientFactory;
            _audioProcessor = audioProcessor;
            _integrationConfigurationManager = integrationConfigurationManager;
        }

        // CURD
        public async Task<bool> CheckAgentExists(long businessId, string agentId)
        {
            return await _businessAppRepository.CheckAgentExists(businessId, agentId);
        }

        public async Task<BusinessAppAgent?> GetAgentById(long businessId, string agentId)
        {
            return await _businessAppRepository.GetAgentById(businessId, agentId); ;
        }

        // SAVING/ADDING AGENT
        public async Task<FunctionReturnResult<BusinessAppAgent?>> AddOrUpdateAgent(
            long businessId,
            string postType,
            IFormCollection formData,
            BusinessAppAgent? exisitingAgentData,
            LLMProviderManager llmProviderManager,
            STTProviderManager sttProviderManager,
            TTSProviderManager ttsProviderManager
        ) {
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

                        newAgentData.Integrations.STT[businessLanguage] = new List<BusinessAppAgentIntegrationData>();

                        var integrationElementArray = sttLanguageElement.EnumerateArray();
                        for (int i = 0; i < integrationElementArray.Count(); i++)
                        {
                            var currentIntegrationElement = integrationElementArray.ElementAt(i);

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                currentIntegrationElement,
                                "STT",
                                businessLanguage
                            );

                            if (!validationBuildResult.Success || validationBuildResult.Data == null)
                            {
                                result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                result.Message = validationBuildResult.Message + $" at index {i}";
                                return result;
                            }

                            newAgentData.Integrations.STT[businessLanguage].Add(validationBuildResult.Data);
                        }                     
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
                            result.Code = "AddOrUpdateAgent:12";
                            result.Message = "LLM section for language " + businessLanguage + " not found. At least one integration is required.";
                            return result;
                        }

                        newAgentData.Integrations.LLM[businessLanguage] = new List<BusinessAppAgentIntegrationData>();

                        var integrationElementArray = llmLanguageElement.EnumerateArray();
                        for (int i = 0; i < integrationElementArray.Count(); i++)
                        {
                            var currentIntegrationElement = integrationElementArray.ElementAt(i);

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                currentIntegrationElement,
                                "LLM",
                                businessLanguage
                            );

                            if (!validationBuildResult.Success || validationBuildResult.Data == null)
                            {
                                result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                result.Message = validationBuildResult.Message + $" at index {i}";
                                return result;
                            }

                            newAgentData.Integrations.LLM[businessLanguage].Add(validationBuildResult.Data);
                        }
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
                            result.Code = "AddOrUpdateAgent:12";
                            result.Message = "TTS section for language " + businessLanguage + " not found. At least one integration is required.";
                            return result;
                        }

                        newAgentData.Integrations.TTS[businessLanguage] = new List<BusinessAppAgentIntegrationData>();

                        var integrationElementArray = ttsLanguageElement.EnumerateArray();
                        for (int i = 0; i < integrationElementArray.Count(); i++)
                        {
                            var currentIntegrationElement = integrationElementArray.ElementAt(i);

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                currentIntegrationElement,
                                "TTS",
                                businessLanguage
                            );

                            if (!validationBuildResult.Success || validationBuildResult.Data == null)
                            {
                                result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                result.Message = validationBuildResult.Message + $" at index {i}";
                                return result;
                            }

                            newAgentData.Integrations.TTS[businessLanguage].Add(validationBuildResult.Data);
                        }
                    }
                }
            }

            // Interruptions
            if (!changesRootElement.TryGetProperty("interruptions", out var interruptionsElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateAgent:INTERRUPTION_MISSING",
                    "Interruptions section not found."
                );
            }
            else
            {
                if (!interruptionsElement.TryGetProperty("turnEnd", out var turnEndElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:INTERRUPTION_TURN_END_MISSING",
                        "Interruptions turn end section not found."
                    );
                }
                else
                {
                    if (!turnEndElement.TryGetProperty("type", out var turnEndTypeElement) ||
                        turnEndTypeElement.ValueKind != JsonValueKind.Number)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:INTERRUPTION_TURN_END_TYPE_INVALID",
                            "Interruptions turn end type not found or invalid."
                        );
                    }
                    var turnEndTypeInt = turnEndTypeElement.GetInt32();
                    if (!Enum.IsDefined(typeof(AgentInterruptionTurnEndTypeENUM), turnEndTypeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:INTERRUPTION_TURN_END_TYPE_UNDEFINED",
                            "Interruptions turn end type not defined."
                        );
                    }
                    newAgentData.Interruptions.TurnEnd.Type = (AgentInterruptionTurnEndTypeENUM)turnEndTypeInt;

                    if (newAgentData.Interruptions.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.VAD)
                    {
                        if (!turnEndElement.TryGetProperty("vadSilenceDurationMS", out var vadSilenceDurationMSElement) ||
                            vadSilenceDurationMSElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:INTERRUPTION_VAD_SILENCE_DURATION_MS_INVALID",
                                "Interruptions VAD silence duration not found or invalid."
                            );
                        }

                        newAgentData.Interruptions.TurnEnd.VadSilenceDurationMS = vadSilenceDurationMSElement.GetInt32();

                        if (newAgentData.Interruptions.TurnEnd.VadSilenceDurationMS <= 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:INTERRUPTION_VAD_SILENCE_DURATION_MS_INVALID_VALUE",
                                "Interruptions VAD silence duration must be greater than 0."
                            );
                        }
                    }
                    else if (newAgentData.Interruptions.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI)
                    {
                        if (!turnEndElement.TryGetProperty("useAgentLLM", out var useAgentLlmElement) || 
                            (useAgentLlmElement.ValueKind != JsonValueKind.False && useAgentLlmElement.ValueKind != JsonValueKind.True))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:INTERRUPTION_USE_AGENT_LLM_INVALID",
                                "Interruptions use agent LLM not found or invalid."
                            );
                        }

                        newAgentData.Interruptions.TurnEnd.UseAgentLLM = useAgentLlmElement.GetBoolean();
                        if (!newAgentData.Interruptions.TurnEnd.UseAgentLLM.Value)
                        {
                            if (!turnEndElement.TryGetProperty("llmIntegration", out var llmIntegrationElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:INTERRUPTION_LLM_INTEGRATION_MISSING",
                                    "Interruptions LLM integration not found."
                                );
                            }

                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                    businessId,
                                    llmIntegrationElement,
                                    "LLM",
                                    null
                                );
                            if (!validationBuildResult.Success)
                            {
                                result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                result.Message = validationBuildResult.Message;
                                return result;
                            }

                            newAgentData.Interruptions.TurnEnd.LLMIntegration = validationBuildResult.Data;
                        }
                    }

                    if (!interruptionsElement.TryGetProperty("useTurnByTurnMode", out var useTurnByTurnModeElement) || 
                        (useTurnByTurnModeElement.ValueKind != JsonValueKind.False && useTurnByTurnModeElement.ValueKind != JsonValueKind.True))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:INTERRUPTION_USE_TURN_BY_TURN_MODE_INVALID",
                            "Interruptions use turn by turn mode not found or invalid."
                        );
                    }
                    else
                    {
                        // IF TURN BY TURN
                        newAgentData.Interruptions.UseTurnByTurnMode = useTurnByTurnModeElement.GetBoolean();
                        if (newAgentData.Interruptions.UseTurnByTurnMode)
                        {
                            if (!interruptionsElement.TryGetProperty("includeInterruptedSpeechInTurnByTurnMode", out var includeInterruptedSpeechInTurnByTurnModeElement) ||
                                (includeInterruptedSpeechInTurnByTurnModeElement.ValueKind != JsonValueKind.False && includeInterruptedSpeechInTurnByTurnModeElement.ValueKind != JsonValueKind.True))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:INTERRUPTION_INCLUDE_INTERRUPTED_SPEECH_IN_TURN_BY_TURN_MODE_INVALID",
                                    "Interruptions include interrupted speech in turn by turn mode not found or invalid."
                                );
                            }
                            else
                            {
                                newAgentData.Interruptions.IncludeInterruptedSpeechInTurnByTurnMode = includeInterruptedSpeechInTurnByTurnModeElement.GetBoolean();
                            }
                        }
                        // IF NOT TURN BY TURN
                        // USE PAUSE TRIGGER AND LLM VERIFICATION
                        else
                        {
                            if (!interruptionsElement.TryGetProperty("pauseTrigger", out var pauseTriggerElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_MISSING",
                                    "Interruptions pause trigger not found."
                                );
                            }
                            else
                            {
                                newAgentData.Interruptions.PauseTrigger = new BusinessAppAgentInterruptionPauseTrigger();

                                if (!pauseTriggerElement.TryGetProperty("type", out var pauseTriggerTypeElement) ||
                                    pauseTriggerTypeElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_TYPE_INVALID",
                                        "Interruptions pause trigger type not found or invalid."
                                    );
                                }
                                var pauseTriggerTypeInt = pauseTriggerTypeElement.GetInt32();
                                if (!Enum.IsDefined(typeof(AgentInterruptionPauseTriggerTypeENUM), pauseTriggerTypeInt))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_TYPE_UNDEFINED",
                                        "Interruptions pause trigger type not defined."
                                    );
                                }
                                newAgentData.Interruptions.PauseTrigger.Type = (AgentInterruptionPauseTriggerTypeENUM)pauseTriggerTypeInt;

                                if (newAgentData.Interruptions.PauseTrigger.Type == AgentInterruptionPauseTriggerTypeENUM.VAD)
                                {
                                    if (!pauseTriggerElement.TryGetProperty("vadDurationMS", out var vadDurationMSElement) ||
                                        vadDurationMSElement.ValueKind != JsonValueKind.Number)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_VAD_DURATION_MS_INVALID",
                                            "Interruptions pause trigger VAD duration not found or invalid."
                                        );
                                    }
                                    newAgentData.Interruptions.PauseTrigger.VadDurationMS = vadDurationMSElement.GetInt32();
                                    if (newAgentData.Interruptions.PauseTrigger.VadDurationMS <= 0)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_VAD_DURATION_MS_INVALID_VALUE",
                                            "Interruptions pause trigger VAD duration must be greater than 0."
                                        );
                                    }
                                }
                                else if (newAgentData.Interruptions.PauseTrigger.Type == AgentInterruptionPauseTriggerTypeENUM.STT)
                                {
                                    if (!pauseTriggerElement.TryGetProperty("wordCount", out var wordCountElement) ||
                                        wordCountElement.ValueKind != JsonValueKind.Number)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_WORD_COUNT_INVALID",
                                            "Interruptions pause trigger word count not found or invalid."
                                        );
                                    }
                                    newAgentData.Interruptions.PauseTrigger.WordCount = wordCountElement.GetInt32();
                                    if (newAgentData.Interruptions.PauseTrigger.WordCount <= 0)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateAgent:INTERRUPTION_PAUSE_TRIGGER_WORD_COUNT_INVALID_VALUE",
                                            "Interruptions pause trigger word count must be greater than 0."
                                        );
                                    }
                                }
                            }

                            if (!interruptionsElement.TryGetProperty("verification", out var verificationElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:INTERRUPTION_VERIFICATION_MISSING",
                                    "Interruptions verification not found."
                                );
                            }
                            else
                            {
                                newAgentData.Interruptions.Verification = new BusinessAppAgentInterruptionVerification();

                                if (!verificationElement.TryGetProperty("enabled", out var enabledElement) ||
                                    (enabledElement.ValueKind != JsonValueKind.True && enabledElement.ValueKind != JsonValueKind.False)
                                )
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateAgent:INTERRUPTION_VERIFICATION_ENABLED_INVALID",
                                        "Interruptions verification enabled not found or invalid."
                                    );
                                }
                                newAgentData.Interruptions.Verification.Enabled = enabledElement.GetBoolean();

                                if (newAgentData.Interruptions.Verification.Enabled)
                                {
                                    if (!verificationElement.TryGetProperty("useAgentLLM", out var useAgentLLMElement) ||
                                        (useAgentLLMElement.ValueKind != JsonValueKind.True && useAgentLLMElement.ValueKind != JsonValueKind.False)
                                    )
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateAgent:INTERRUPTION_VERIFICATION_USE_AGENT_LLM_INVALID",
                                            "Interruptions verification use agent LLM not found or invalid."
                                        );
                                    }
                                    newAgentData.Interruptions.Verification.UseAgentLLM = useAgentLLMElement.GetBoolean();

                                    if (!newAgentData.Interruptions.Verification.UseAgentLLM)
                                    {
                                        if (!verificationElement.TryGetProperty("llmIntegration", out var llmIntegrationElement) ||
                                            llmIntegrationElement.ValueKind != JsonValueKind.Object)
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateAgent:INTERRUPTION_VERIFICATION_LLM_INTEGRATION_INVALID",
                                                "Interruptions verification LLM integration not found or invalid."
                                            );
                                        }

                                        var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                            businessId,
                                            llmIntegrationElement,
                                            "LLM",
                                            null
                                        );
                                        if (!validationBuildResult.Success)
                                        {
                                            result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                            result.Message = validationBuildResult.Message;
                                            return result;
                                        }

                                        newAgentData.Interruptions.Verification.LLMIntegration = validationBuildResult.Data;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Knowledge Base
            if (!changesRootElement.TryGetProperty("knowledgeBase", out var knowledgeBaseElement))
            {
                return result.SetFailureResult(
                    "AddOrUpdateAgent:KNOWLEDGE_BASE_NOT_FOUND",
                    "Knowledge base section not found."
                );
            }
            else
            {
                if (!knowledgeBaseElement.TryGetProperty("linkedGroups", out var linkedGroupsElement) ||
                    linkedGroupsElement.ValueKind != JsonValueKind.Array)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:KNOWLEDGE_BASE_LINKED_GROUPS_NOT_FOUND",
                        "Knowledge base linked groups not found."
                    );
                }
                else
                {
                    var linkedGroupsEnumerateArray = linkedGroupsElement.EnumerateArray();
                    for (int i = 0; i < linkedGroupsEnumerateArray.Count(); i++)
                    {
                        var element = linkedGroupsEnumerateArray.ElementAt(i);
                        if (element.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_LINKED_GROUPS_INVALID_TYPE",
                                $"Invalid array item type for knowledge base linked groups at index {i}. Found: {element.ValueKind}"
                            );
                        }

                        var linkedGroupId = element.GetString();
                        if (string.IsNullOrWhiteSpace(linkedGroupId))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_LINKED_GROUPS_EMPTY",
                                $"Empty array item type for knowledge base linked groups at index {i}."
                            );
                        }

                        var kbGExists = await _parentBusinessManager.GetKnowledgeBaseManager().CheckKnowledgeBaseGroupExistsById(businessId, linkedGroupId);
                        if (!kbGExists)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_LINKED_GROUPS_NOT_FOUND",
                                $"Linked knowledge base group {linkedGroupId} not found for business."
                            );
                        }

                        newAgentData.KnowledgeBase.LinkedGroups.Add(linkedGroupId);
                    }
                }

                if (!knowledgeBaseElement.TryGetProperty("searchStrategy", out var searchStrategyElement) ||
                    searchStrategyElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_NOT_FOUND",
                        "Knowledge base search strategy not found."
                    );
                }
                else
                {
                    if (!searchStrategyElement.TryGetProperty("type", out var typeElement) ||
                        typeElement.ValueKind != JsonValueKind.Number)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_TYPE_NOT_FOUND",
                            "Knowledge base search strategy type not found."
                        );
                    }
                    var typeInt = typeElement.GetInt32();
                    if (!Enum.IsDefined(typeof(AgentKnowledgeBaseSearchStartegyTypeENUM), typeInt))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_TYPE_INVALID",
                            "Knowledge base search strategy type is invalid."
                        );
                    }
                    newAgentData.KnowledgeBase.SearchStrategy.Type = (AgentKnowledgeBaseSearchStartegyTypeENUM)typeInt;

                    if (newAgentData.KnowledgeBase.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.SpecificKeyword)
                    {
                        if (!searchStrategyElement.TryGetProperty("specificKeywords", out var specificKeywordsElement) ||
                            specificKeywordsElement.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_SPECIFIC_KEYWORDS_NOT_FOUND",
                                "Knowledge base search strategy specific keywords not found."
                            );
                        }
                        var specificKeywords = specificKeywordsElement.GetString();
                        if (string.IsNullOrWhiteSpace(specificKeywords))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_SPECIFIC_KEYWORDS_EMPTY",
                                "Knowledge base search strategy specific keywords is empty."
                            );
                        }
                        var splitSpecificKeywords = specificKeywords.Split(',').ToList();
                        if (splitSpecificKeywords.Count == 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_SPECIFIC_KEYWORDS_SPLIT_EMPTY",
                                "Knowledge base search strategy specific keywords is empty after comma split."
                            );
                        }
                        newAgentData.KnowledgeBase.SearchStrategy.SpecificKeywords = splitSpecificKeywords;
                    }
                    else if (newAgentData.KnowledgeBase.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.LLM)
                    {
                        newAgentData.KnowledgeBase.SearchStrategy.LLMClassifier = new();

                        if (!searchStrategyElement.TryGetProperty("llmClassifier", out var llmClassifierElement) ||
                            llmClassifierElement.ValueKind != JsonValueKind.Object)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_LLM_CLASSIFIER_NOT_FOUND",
                                "Knowledge base search strategy LLM classifier not found."
                            );
                        }
                        else
                        {
                            if (
                                !llmClassifierElement.TryGetProperty("useAgentLLM", out var useAgentLLMElement) ||
                                (useAgentLLMElement.ValueKind != JsonValueKind.True && useAgentLLMElement.ValueKind != JsonValueKind.False)
                            ) {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_LLM_CLASSIFIER_USE_AGENT_LLM_NOT_FOUND",
                                    "Knowledge base search strategy LLM classifier use agent LLM not found."
                                );
                            }
                            newAgentData.KnowledgeBase.SearchStrategy.LLMClassifier.UseAgentLLM = useAgentLLMElement.GetBoolean();

                            if (!newAgentData.KnowledgeBase.SearchStrategy.LLMClassifier.UseAgentLLM)
                            {
                                if (!llmClassifierElement.TryGetProperty("llmIntegration", out var llmIntegrationElement) ||
                                    llmIntegrationElement.ValueKind != JsonValueKind.Object)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateAgent:KNOWLEDGE_BASE_SEARCH_STRATEGY_LLM_CLASSIFIER_LLM_INTEGRATION_NOT_FOUND",
                                        "Knowledge base search strategy LLM classifier LLM integration not found."
                                    );
                                }
                                var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                    businessId,
                                    llmIntegrationElement,
                                    "LLM",
                                    null
                                );
                                if (!validationBuildResult.Success)
                                {
                                    result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                    result.Message = validationBuildResult.Message;
                                    return result;
                                }
                                newAgentData.KnowledgeBase.SearchStrategy.LLMClassifier.LLMIntegration = validationBuildResult.Data;
                            }
                        }
                    }
                }

                if (!knowledgeBaseElement.TryGetProperty("refinement", out var refinementElement) ||
                    refinementElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:KNOWLEDGE_BASE_REFINEMENT_NOT_FOUND",
                        "Knowledge base refinement not found."
                    );
                }
                else
                {
                    if (!refinementElement.TryGetProperty("enabled", out var enabledElement) ||
                        (enabledElement.ValueKind != JsonValueKind.True && enabledElement.ValueKind != JsonValueKind.False)
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:KNOWLEDGE_BASE_REFINEMENT_ENABLED_NOT_FOUND",
                            "Knowledge base refinement enabled not found."
                        );
                    }
                    newAgentData.KnowledgeBase.Refinement.Enabled = enabledElement.GetBoolean();

                    if (newAgentData.KnowledgeBase.Refinement.Enabled)
                    {
                        if (!refinementElement.TryGetProperty("queryCount", out var queryCountElement) ||
                            queryCountElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_REFINEMENT_QUERY_COUNT_NOT_FOUND",
                                "Knowledge base refinement query count not found."
                            );
                        }
                        newAgentData.KnowledgeBase.Refinement.QueryCount = queryCountElement.GetInt32();

                        if (!refinementElement.TryGetProperty("useAgentLLM", out var useAgentLLMElement) ||
                            (useAgentLLMElement.ValueKind != JsonValueKind.True && useAgentLLMElement.ValueKind != JsonValueKind.False)
                        ) {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:KNOWLEDGE_BASE_REFINEMENT_USE_AGENT_LLM_NOT_FOUND",
                                "Knowledge base refinement use agent LLM not found."
                            );
                        }
                        newAgentData.KnowledgeBase.Refinement.UseAgentLLM = useAgentLLMElement.GetBoolean();

                        if (!newAgentData.KnowledgeBase.Refinement.UseAgentLLM.Value)
                        {
                            if (!refinementElement.TryGetProperty("llmIntegration", out var llmIntegrationElement) ||
                                llmIntegrationElement.ValueKind != JsonValueKind.Object)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateAgent:KNOWLEDGE_BASE_REFINEMENT_LLM_INTEGRATION_NOT_FOUND",
                                    "Knowledge base refinement LLM integration not found."
                                );
                            }
                            var validationBuildResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(
                                businessId,
                                llmIntegrationElement,
                                "LLM",
                                null
                            );
                            if (!validationBuildResult.Success)
                            {
                                result.Code = "AddOrUpdateAgent:" + validationBuildResult.Code;
                                result.Message = validationBuildResult.Message;
                                return result;
                            }
                            newAgentData.KnowledgeBase.Refinement.LLMIntegration = validationBuildResult.Data;
                        }
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
                // Messages
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

                // Audios
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

                // Audio Settings
                if (!cacheTabElement.TryGetProperty("audioCacheSettings", out var audioCacheSettingsElement)
                    || audioCacheSettingsElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                            "AddOrUpdateAgent:CACHE_AUDIOCACHESETTINGS_INVALID",
                            "Cache audioCacheSettings parameter is missing or invalid."
                        );
                }
                else
                {
                    var audioSettings = new BusinessAppAgentAutoCacheAudioSettings();

                    if (!audioCacheSettingsElement.TryGetProperty("autoCacheAudioResponses", out var autoCacheAudioEnabledElement) ||
                        (autoCacheAudioEnabledElement.ValueKind != JsonValueKind.True && autoCacheAudioEnabledElement.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:CACHE_AUTO_CACHE_AUDIO_ENABLED_INVALID",
                            "Cache autoCacheAudioResponses parameter is missing or invalid."
                        );
                    }
                    audioSettings.AutoCacheAudioResponses = autoCacheAudioEnabledElement.GetBoolean();

                    if (audioSettings.AutoCacheAudioResponses)
                    {
                        if (!audioCacheSettingsElement.TryGetProperty("autoCacheAudioResponseCacheGroupId", out var groupIdElement)
                            || groupIdElement.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:AUDIO_CACHE_GROUPID_INVALID",
                                "Cache autoCacheAudioResponseCacheGroupId parameter is missing or invalid."
                            );
                        }
                        var cacheGroupId = groupIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(cacheGroupId))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:AUDIO_CACHE_GROUPID_EMPTY",
                                "An audio cache group must be selected when auto-caching is enabled."
                            );
                        }

                        var checkAudioCacheGroupExistsResult = await _parentBusinessManager.GetCacheManager().CheckBusinessCacheAudioGroupExists(businessId, cacheGroupId);
                        if (!checkAudioCacheGroupExistsResult)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:AUDIO_CACHE_GROUPID_NOTFOUND",
                                $"The selected auto-cache audio group (ID: {cacheGroupId}) does not exist."
                            );
                        }
                        audioSettings.AutoCacheAudioResponseCacheGroupId = cacheGroupId;

                        if (!audioCacheSettingsElement.TryGetProperty("autoCacheAudioResponsesDefaultExpiryHours", out var expiryElement) || expiryElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:AUDIO_CACHE_EXPIRY_INVALID",
                                "Cache autoCacheAudioResponsesDefaultExpiryHours parameter is missing or invalid."
                            );
                        }
                        var expiryHours = expiryElement.GetInt32();
                        if (expiryHours < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:AUDIO_CACHE_EXPIRY_NEGATIVE",
                                "Cache expiry hours cannot be negative."
                            );
                        }
                        audioSettings.AutoCacheAudioResponsesDefaultExpiryHours = expiryHours;
                    }

                    newAgentData.Cache.AudioCacheSettings = audioSettings;
                }

                // Embeddings
                if (!cacheTabElement.TryGetProperty("embeddings", out var embeddingsElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:CACHE_EMBEDDINGS_INVALID",
                        "Cache embeddings parameter is missing or invalid."
                    );
                }
                else
                {
                    foreach (var embeddingCacheIdElement in embeddingsElement.EnumerateArray())
                    {
                        if (embeddingCacheIdElement.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:CACHE_EMBEDDINGS_ITEM_INVALID",
                                "Invalid array item type for cache embeddings. Found: " + embeddingCacheIdElement.ValueKind
                            );
                        }

                        var embeddingsCacheGroupId = embeddingCacheIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(embeddingsCacheGroupId))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:CACHE_EMBEDDINGS_ITEM_EMPTY",
                                "Empty array item type for cache embeddings."
                            );
                        }

                        var checkEmbeddingCacheGroupExistsResult = await _parentBusinessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, embeddingsCacheGroupId);
                        if (!checkEmbeddingCacheGroupExistsResult)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:CACHE_EMBEDDINGS_ITEM_NOT_FOUND",
                                $"Cache embedding group does not exist with id: {embeddingsCacheGroupId}"
                            );
                        }

                        newAgentData.Cache.Embeddings.Add(embeddingsCacheGroupId);
                    }
                }

                // Embedding Settings
                if (!cacheTabElement.TryGetProperty("embeddingsCacheSettings", out var embeddingsCacheSettingsElement)
                    || embeddingsCacheSettingsElement.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                            "AddOrUpdateAgent:CACHE_EMBEDDING_CACHE_SETTINGS_INVALID",
                            "Cache embeddingsCacheSettings parameter is missing or invalid."
                        );
                }
                else
                {
                    var embeddingSettings = new BusinessAppAgentAutoCacheEmbeddingsSettings();

                    if (!embeddingsCacheSettingsElement.TryGetProperty("autoCacheEmbeddingResponses", out var autoCacheEmbeddingEnabledElement) ||
                        (autoCacheEmbeddingEnabledElement.ValueKind != JsonValueKind.True && autoCacheEmbeddingEnabledElement.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:CACHE_AUTO_CACHE_EMBEDDING_ENABLED_INVALID",
                            "Cache autoCacheEmbeddingResponses parameter is missing or invalid."
                        );
                    }
                    embeddingSettings.AutoCacheEmbeddingResponses = autoCacheEmbeddingEnabledElement.GetBoolean();

                    if (embeddingSettings.AutoCacheEmbeddingResponses)
                    {
                        if (!embeddingsCacheSettingsElement.TryGetProperty("autoCacheEmbeddingResponseCacheGroupId", out var embeddingGroupIdElement)
                            || embeddingGroupIdElement.ValueKind != JsonValueKind.String)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:EMBEDDING_CACHE_GROUPID_INVALID",
                                "Cache autoCacheEmbeddingResponseCacheGroupId parameter is missing or invalid."
                            );
                        }
                        var embeddingCacheGroupId = embeddingGroupIdElement.GetString();
                        if (string.IsNullOrWhiteSpace(embeddingCacheGroupId))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:CACHE_GROUPID_EMPTY",
                                "An embedding cache group must be selected when auto-caching is enabled."
                            );
                        }

                        var checkEmbeddingCacheGroupExistsResult = await _parentBusinessManager.GetCacheManager().CheckBusinessCacheEmbeddingGroupExists(businessId, embeddingCacheGroupId);
                        if (!checkEmbeddingCacheGroupExistsResult)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:EMBEDDING_CACHE_GROUPID_NOTFOUND",
                                $"The selected auto-cache embedding group (ID: {embeddingCacheGroupId}) does not exist."
                            );
                        }
                        embeddingSettings.AutoCacheEmbeddingResponseCacheGroupId = embeddingCacheGroupId;

                        if (!embeddingsCacheSettingsElement.TryGetProperty("autoCacheEmbeddingResponsesDefaultExpiryHours", out var expiryElement) || expiryElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:EMBEDDING_CACHE_EXPIRY_INVALID",
                                "Cache autoCacheEmbeddingResponsesDefaultExpiryHours parameter is missing or invalid."
                            );
                        }
                        var expiryHours = expiryElement.GetInt32();
                        if (expiryHours < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:EMBEDDING_CACHE_EXPIRY_NEGATIVE",
                                "Embedding cache expiry hours cannot be negative."
                            );
                        }
                        embeddingSettings.AutoCacheEmbeddingResponsesDefaultExpiryHours = expiryHours;
                    }

                    newAgentData.Cache.EmbeddingsCacheSettings = embeddingSettings;
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

                        newAgentData.Settings.BackgroundAudioS3StorageLink = new S3StorageFileLink
                        {
                            ObjectName = validationResult.Hash,
                            OriginRegion = _s3StorageClientFactory.GetCurrentRegion()
                        };
                    }
                    else if (backgroundAudioUrl == "previous")
                    {
                        if (postType != "edit")
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:PREVIOUS_BACKGROUND_AUDIO_URL_INVALID",
                                "Invalid background audio url type. Previous is only allowed when editing an agent."
                            );
                        }

                        var exisitingAgentSettingsBackgroundAudioS3StorageLink = await _businessAppRepository.GetAgentSettingsBackgroundAudioS3StorageLink(businessId, exisitingAgentData!.Id);
                        if (exisitingAgentSettingsBackgroundAudioS3StorageLink == null)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:PREVIOUS_BACKGROUND_AUDIO_URL_NOTFOUND",
                                "Previous background audio url not found."
                            );
                        }
                        newAgentData.Settings.BackgroundAudioS3StorageLink = exisitingAgentSettingsBackgroundAudioS3StorageLink;
                    }
                    else
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:INVALID_BACKGROUND_AUDIO_URL_TYPE",
                            "Invalid background audio url type (allowed custom or previous)."
                        );
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

            using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                try
                {
                    if (postType == "new")
                    {
                        newAgentData.Id = ObjectId.GenerateNewId().ToString();

                        var addAgentResult = await _businessAppRepository.AddAgent(businessId, newAgentData, session);
                        if (!addAgentResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:DB_ADD_FAILED",
                                "Failed to add business agent."
                            );
                        }
                    }
                    else if (postType == "edit")
                    {
                        newAgentData.Id = exisitingAgentData!.Id;

                        var updateAgentResult = await _businessAppRepository.UpdateAgentDataExceptReferences(businessId, newAgentData, session);
                        if (!updateAgentResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateAgent:DB_UPDATE_FAILED",
                                "Failed to update business agent."
                            );
                        }
                    }

                    // UPDATE ALL REFERENCES
                    try
                    {
                        await UpdateAllAgentReferences(businessId, newAgentData.Id, exisitingAgentData, newAgentData, session);
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateAgent:REF_UPDATE_FAILED",
                            $"Failed to update agent references: {ex.Message}"
                        );
                    }

                    await session.CommitTransactionAsync();
                    return result.SetSuccessResult(newAgentData);
                }
                catch (Exception ex)
                {
                    await session.AbortTransactionAsync();
                    return result.SetFailureResult(
                        "AddOrUpdateAgent:EXCEPTION",
                        $"An unexpected error occurred: {ex.Message}"
                    );
                }
            }
        }
        private async Task UpdateAllAgentReferences(
            long businessId,
            string agentId,
            BusinessAppAgent? oldAgent,
            BusinessAppAgent newAgent,
            IClientSessionHandle session
        ) {
            // ==================================================================================
            // HELPERS
            // ==================================================================================

            // Helper 1: For List<string> references (e.g. Linked Groups, Cache Lists)
            async Task HandleListReferences(
                List<string>? oldList,
                List<string> newList,
                Func<long, string, string, IClientSessionHandle, Task<bool>> addFunc,
                Func<long, string, string, IClientSessionHandle, Task<bool>> removeFunc)
            {
                var oldSet = oldList != null ? new HashSet<string>(oldList) : new HashSet<string>();
                var newSet = new HashSet<string>(newList);

                // Remove Deleted
                foreach (var oldId in oldSet)
                {
                    if (!newSet.Contains(oldId))
                    {
                        await removeFunc(businessId, oldId, agentId, session);
                    }
                }

                // Add New
                foreach (var newId in newSet)
                {
                    await addFunc(businessId, newId, agentId, session);
                }
            }

            // Helper 2: For Single ID references (e.g. AutoCache Group, Single Integrations)
            async Task HandleSingleReference(
                string? oldId,
                string? newId,
                Func<long, string, string, IClientSessionHandle, Task<bool>> addFunc,
                Func<long, string, string, IClientSessionHandle, Task<bool>> removeFunc)
            {
                // If we have an old reference and it's different from the new one, remove it
                if (!string.IsNullOrEmpty(oldId) && oldId != newId)
                {
                    await removeFunc(businessId, oldId, agentId, session);
                }

                // Always add the new one (idempotent via AddToSet)
                if (!string.IsNullOrEmpty(newId))
                {
                    await addFunc(businessId, newId, agentId, session);
                }
            }

            // Helper 3: For Multi-Language Dictionary references (STT, LLM, TTS)
            async Task HandleMultiLangReferences(
                Dictionary<string, List<BusinessAppAgentIntegrationData>>? oldDict,
                Dictionary<string, List<BusinessAppAgentIntegrationData>> newDict,
                Func<long, string, string, string, IClientSessionHandle, Task<bool>> addFunc,
                Func<long, string, string, string, IClientSessionHandle, Task<bool>> removeFunc)
            {
                // 1. Remove references that are in OLD but NOT in NEW
                if (oldDict != null)
                {
                    foreach (var langPair in oldDict)
                    {
                        string lang = langPair.Key;
                        var oldIntegrations = langPair.Value;

                        // If the language still exists in new data
                        if (newDict.TryGetValue(lang, out var newIntegrations))
                        {
                            var newIds = newIntegrations.Select(x => x.Id).ToHashSet();
                            foreach (var oldInt in oldIntegrations)
                            {
                                // If old ID is not in the new list for this language, remove it
                                if (!newIds.Contains(oldInt.Id))
                                {
                                    await removeFunc(businessId, oldInt.Id, lang, agentId, session);
                                }
                            }
                        }
                        else
                        {
                            // Language was completely removed? Remove all old integrations for this language
                            foreach (var oldInt in oldIntegrations)
                            {
                                await removeFunc(businessId, oldInt.Id, lang, agentId, session);
                            }
                        }
                    }
                }

                // 2. Add all NEW references
                foreach (var langPair in newDict)
                {
                    string lang = langPair.Key;
                    foreach (var newInt in langPair.Value)
                    {
                        await addFunc(businessId, newInt.Id, lang, agentId, session);
                    }
                }
            }

            // ==================================================================================
            // PART 1: ENTITY REFERENCES (KB, CACHE)
            // ==================================================================================

            // 1. Knowledge Base
            await HandleListReferences(
                oldAgent?.KnowledgeBase.LinkedGroups,
                newAgent.KnowledgeBase.LinkedGroups,
                _businessAppRepository.AddAgentReferenceToKB,
                _businessAppRepository.RemoveAgentReferenceFromKB
            );

            // 2. Cache - Message Groups
            await HandleListReferences(
                oldAgent?.Cache.Messages,
                newAgent.Cache.Messages,
                _businessAppRepository.AddAgentReferenceToMessageCacheGroup,
                _businessAppRepository.RemoveAgentReferenceFromMessageCacheGroup
            );

            // 3. Cache - Audio Groups (Usage List)
            await HandleListReferences(
                oldAgent?.Cache.Audios,
                newAgent.Cache.Audios,
                _businessAppRepository.AddAgentReferenceToAudioCacheGroup,
                _businessAppRepository.RemoveAgentReferenceFromAudioCacheGroup
            );

            // 4. Cache - Audio Auto Cache (Single Reference)
            string? oldAudioAutoId = (oldAgent?.Cache.AudioCacheSettings != null && oldAgent.Cache.AudioCacheSettings.AutoCacheAudioResponses)
                ? oldAgent.Cache.AudioCacheSettings.AutoCacheAudioResponseCacheGroupId : null;
            string? newAudioAutoId = (newAgent.Cache.AudioCacheSettings != null && newAgent.Cache.AudioCacheSettings.AutoCacheAudioResponses)
                ? newAgent.Cache.AudioCacheSettings.AutoCacheAudioResponseCacheGroupId : null;

            await HandleSingleReference(
                oldAudioAutoId,
                newAudioAutoId,
                _businessAppRepository.AddAgentAutoCacheReferenceToAudioCacheGroup,
                _businessAppRepository.RemoveAgentAutoCacheReferenceFromAudioCacheGroup
            );

            // 5. Cache - Embedding Groups (Usage List)
            await HandleListReferences(
                oldAgent?.Cache.Embeddings,
                newAgent.Cache.Embeddings,
                _businessAppRepository.AddAgentReferenceToEmbeddingCacheGroup,
                _businessAppRepository.RemoveAgentReferenceFromEmbeddingCacheGroup
            );

            // 6. Cache - Embedding Auto Cache (Single Reference)
            string? oldEmbeddingAutoId = (oldAgent?.Cache.EmbeddingsCacheSettings != null && oldAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponses)
                ? oldAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponseCacheGroupId : null;
            string? newEmbeddingAutoId = (newAgent.Cache.EmbeddingsCacheSettings != null && newAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponses)
                ? newAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponseCacheGroupId : null;

            await HandleSingleReference(
                oldEmbeddingAutoId,
                newEmbeddingAutoId,
                _businessAppRepository.AddAgentAutoCacheReferenceToEmbeddingCacheGroup,
                _businessAppRepository.RemoveAgentAutoCacheReferenceFromEmbeddingCacheGroup
            );


            // ==================================================================================
            // PART 2: INTEGRATION REFERENCES
            // ==================================================================================

            // 7. Interruption Turn End
            string? oldTurnEndId = null;
            if (oldAgent?.Interruptions.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI && !oldAgent.Interruptions.TurnEnd.UseAgentLLM.GetValueOrDefault())
            {
                oldTurnEndId = oldAgent.Interruptions.TurnEnd.LLMIntegration?.Id;
            }

            string? newTurnEndId = null;
            if (newAgent.Interruptions.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI && !newAgent.Interruptions.TurnEnd.UseAgentLLM.GetValueOrDefault())
            {
                newTurnEndId = newAgent.Interruptions.TurnEnd.LLMIntegration?.Id;
            }

            await HandleSingleReference(
                oldTurnEndId,
                newTurnEndId,
                _businessAppRepository.AddAgentInterruptionTurnEndRefToIntegration,
                _businessAppRepository.RemoveAgentInterruptionTurnEndRefFromIntegration
            );

            // 8. Interruption Verification
            string? oldVerifId = null;
            if (oldAgent?.Interruptions.Verification != null && oldAgent.Interruptions.Verification.Enabled && !oldAgent.Interruptions.Verification.UseAgentLLM)
            {
                oldVerifId = oldAgent.Interruptions.Verification.LLMIntegration?.Id;
            }

            string? newVerifId = null;
            if (newAgent.Interruptions.Verification != null && newAgent.Interruptions.Verification.Enabled && !newAgent.Interruptions.Verification.UseAgentLLM)
            {
                newVerifId = newAgent.Interruptions.Verification.LLMIntegration?.Id;
            }

            await HandleSingleReference(
                oldVerifId,
                newVerifId,
                _businessAppRepository.AddAgentInterruptionVerificationRefToIntegration,
                _businessAppRepository.RemoveAgentInterruptionVerificationRefFromIntegration
            );

            // 9. KB Refinement
            string? oldRefineId = null;
            if (oldAgent?.KnowledgeBase.Refinement.Enabled == true && !oldAgent.KnowledgeBase.Refinement.UseAgentLLM.GetValueOrDefault())
            {
                oldRefineId = oldAgent.KnowledgeBase.Refinement.LLMIntegration?.Id;
            }

            string? newRefineId = null;
            if (newAgent.KnowledgeBase.Refinement.Enabled && !newAgent.KnowledgeBase.Refinement.UseAgentLLM.GetValueOrDefault())
            {
                newRefineId = newAgent.KnowledgeBase.Refinement.LLMIntegration?.Id;
            }

            await HandleSingleReference(
                oldRefineId,
                newRefineId,
                _businessAppRepository.AddAgentKBQueryRefinementRefToIntegration,
                _businessAppRepository.RemoveAgentKBQueryRefinementRefFromIntegration
            );

            // 10. KB Search Strategy (LLM Classifier)
            string? oldSearchId = null;
            if (oldAgent?.KnowledgeBase.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.LLM &&
                oldAgent.KnowledgeBase.SearchStrategy.LLMClassifier != null &&
                !oldAgent.KnowledgeBase.SearchStrategy.LLMClassifier.UseAgentLLM)
            {
                oldSearchId = oldAgent.KnowledgeBase.SearchStrategy.LLMClassifier.LLMIntegration?.Id;
            }

            string? newSearchId = null;
            if (newAgent.KnowledgeBase.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.LLM &&
                newAgent.KnowledgeBase.SearchStrategy.LLMClassifier != null &&
                !newAgent.KnowledgeBase.SearchStrategy.LLMClassifier.UseAgentLLM)
            {
                newSearchId = newAgent.KnowledgeBase.SearchStrategy.LLMClassifier.LLMIntegration?.Id;
            }

            await HandleSingleReference(
                oldSearchId,
                newSearchId,
                _businessAppRepository.AddAgentKBSearchStrategyRefToIntegration,
                _businessAppRepository.RemoveAgentKBSearchStrategyRefFromIntegration
            );


            // ==================================================================================
            // PART 3: MULTI-LANGUAGE INTEGRATION REFERENCES
            // ==================================================================================

            // 11. STT
            await HandleMultiLangReferences(
                oldAgent?.Integrations.STT,
                newAgent.Integrations.STT,
                _businessAppRepository.AddAgentSTTReferenceToIntegration,
                _businessAppRepository.RemoveAgentSTTReferenceFromIntegration
            );

            // 12. LLM
            await HandleMultiLangReferences(
                oldAgent?.Integrations.LLM,
                newAgent.Integrations.LLM,
                _businessAppRepository.AddAgentLLMReferenceToIntegration,
                _businessAppRepository.RemoveAgentLLMReferenceFromIntegration
            );

            // 13. TTS
            await HandleMultiLangReferences(
                oldAgent?.Integrations.TTS,
                newAgent.Integrations.TTS,
                _businessAppRepository.AddAgentTTSReferenceToIntegration,
                _businessAppRepository.RemoveAgentTTSReferenceFromIntegration
            );
        }

        // Delete Agent
        public async Task<FunctionReturnResult> DeleteAgent(long businessId, BusinessAppAgent businessAppAgent)
        {
            var result = new FunctionReturnResult();

            try
            {
                if (businessAppAgent.InboundRoutingReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteAgent:INBOUND_ROUTING_REFERENCES",
                        "Cannot delete agent with inbound routing references."
                    );
                }

                if (businessAppAgent.TelephonyCampaignReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteAgent:TELEPHONY_CAMPAIGN_REFERENCES",
                        "Cannot delete agent with telephony campaign references."
                    );
                }

                if (businessAppAgent.WebCampaignReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteAgent:WEB_CAMPAIGN_REFERENCES",
                        "Cannot delete agent with web campaign references."
                    );
                }

                if (businessAppAgent.ScriptTransferToAgentNodeReferences.Count > 0)
                {
                    return result.SetFailureResult(
                        "DeleteAgent:SCRIPT_TRANSFER_TO_AGENT_NODE_REFERENCES",
                        "Cannot delete agent with script transfer to agent node references."
                    );
                }

                // TODO check any ongoing queues/conversations with this agent
                // too complex allow for queues or conversations to just fail gracefully

                using (var session = await _mongoClient.StartSessionAsync())
                {
                    session.StartTransaction();
                    try
                    {
                        // Interruption Turn End
                        if (businessAppAgent.Interruptions.TurnEnd.Type == AgentInterruptionTurnEndTypeENUM.AI &&
                            !businessAppAgent.Interruptions.TurnEnd.UseAgentLLM.GetValueOrDefault() &&
                            businessAppAgent.Interruptions.TurnEnd.LLMIntegration != null)
                        {
                            var removeTurnEndRefResult =  await _businessAppRepository.RemoveAgentInterruptionTurnEndRefFromIntegration(
                                businessId,
                                businessAppAgent.Interruptions.TurnEnd.LLMIntegration.Id,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeTurnEndRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_TURN_END_REF",
                                    "Failed to remove interruption turn end integration reference."
                                );
                            }
                        }

                        // Interruption Verification
                        if (businessAppAgent.Interruptions.Verification != null &&
                            businessAppAgent.Interruptions.Verification.Enabled &&
                            !businessAppAgent.Interruptions.Verification.UseAgentLLM &&
                            businessAppAgent.Interruptions.Verification.LLMIntegration != null)
                        {
                            var removeInterruptionVerificationRefResult = await _businessAppRepository.RemoveAgentInterruptionVerificationRefFromIntegration(
                                businessId,
                                businessAppAgent.Interruptions.Verification.LLMIntegration.Id,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeInterruptionVerificationRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_INTERRUPTION_VERIFICATION_REF",
                                    "Failed to remove interruption verification integration reference."
                                );
                            }
                        }

                        // Knowledge Base Refinement
                        if (businessAppAgent.KnowledgeBase.Refinement.Enabled &&
                            !businessAppAgent.KnowledgeBase.Refinement.UseAgentLLM.GetValueOrDefault() &&
                            businessAppAgent.KnowledgeBase.Refinement.LLMIntegration != null)
                        {
                            var removeRefinementRefResult = await _businessAppRepository.RemoveAgentKBQueryRefinementRefFromIntegration(
                                businessId,
                                businessAppAgent.KnowledgeBase.Refinement.LLMIntegration.Id,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeRefinementRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_REFINEMENT_REF",
                                    "Failed to remove knowledge base query refinement integration reference."
                                );
                            }
                        }

                        // Knowledge Base Search Strategy (LLM Classifier)
                        if (businessAppAgent.KnowledgeBase.SearchStrategy.Type == AgentKnowledgeBaseSearchStartegyTypeENUM.LLM &&
                            businessAppAgent.KnowledgeBase.SearchStrategy.LLMClassifier != null &&
                            !businessAppAgent.KnowledgeBase.SearchStrategy.LLMClassifier.UseAgentLLM &&
                            businessAppAgent.KnowledgeBase.SearchStrategy.LLMClassifier.LLMIntegration != null)
                        {
                            var removeLLMClassifierRefResult = await _businessAppRepository.RemoveAgentKBSearchStrategyRefFromIntegration(
                                businessId,
                                businessAppAgent.KnowledgeBase.SearchStrategy.LLMClassifier.LLMIntegration.Id,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeLLMClassifierRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_LLM_CLASSIFIER_REF",
                                    "Failed to remove knowledge base search strategy integration reference."
                                );
                            }
                        }

                        // STT
                        if (businessAppAgent.Integrations.STT != null)
                        {
                            foreach (var langPair in businessAppAgent.Integrations.STT)
                            {
                                foreach (var integration in langPair.Value)
                                {
                                    var removeSTTRefResult = await _businessAppRepository.RemoveAgentSTTReferenceFromIntegration(
                                        businessId,
                                        integration.Id,
                                        langPair.Key,
                                        businessAppAgent.Id,
                                        session
                                    );
                                    if (!removeSTTRefResult)
                                    {
                                        await session.AbortTransactionAsync();
                                        return result.SetFailureResult(
                                            "DeleteAgent:REMOVE_STT_REF",
                                            "Failed to remove STT integration reference."
                                        );
                                    }
                                }
                            }
                        }

                        // LLM
                        if (businessAppAgent.Integrations.LLM != null)
                        {
                            foreach (var langPair in businessAppAgent.Integrations.LLM)
                            {
                                foreach (var integration in langPair.Value)
                                {
                                    var removeLLMRefResult = await _businessAppRepository.RemoveAgentLLMReferenceFromIntegration(
                                        businessId,
                                        integration.Id,
                                        langPair.Key,
                                        businessAppAgent.Id,
                                        session
                                    );
                                    if (!removeLLMRefResult)
                                    {
                                        await session.AbortTransactionAsync();
                                        return result.SetFailureResult(
                                            "DeleteAgent:REMOVE_LLM_REF",
                                            "Failed to remove LLM integration reference."
                                        );
                                    }
                                }
                            }
                        }

                        // TTS
                        if (businessAppAgent.Integrations.TTS != null)
                        {
                            foreach (var langPair in businessAppAgent.Integrations.TTS)
                            {
                                foreach (var integration in langPair.Value)
                                {
                                    var removeTTSRefResult = await _businessAppRepository.RemoveAgentTTSReferenceFromIntegration(
                                        businessId,
                                        integration.Id,
                                        langPair.Key,
                                        businessAppAgent.Id,
                                        session
                                    );
                                    if (!removeTTSRefResult)
                                    {
                                        await session.AbortTransactionAsync();
                                        return result.SetFailureResult(
                                            "DeleteAgent:REMOVE_TTS_REF",
                                            "Failed to remove TTS integration reference."
                                        );
                                    }
                                }
                            }
                        }

                        // Knowledge Base
                        foreach (var kbId in businessAppAgent.KnowledgeBase.LinkedGroups)
                        {
                            var removeKBRefResult = await _businessAppRepository.RemoveAgentReferenceFromKB(businessId, kbId, businessAppAgent.Id, session);
                            if (!removeKBRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_KB_REF",
                                    "Failed to remove knowledge base reference."
                                );
                            }
                        }

                        // Cache - Messages
                        foreach (var msgGroupId in businessAppAgent.Cache.Messages)
                        {
                            var removeMsgGroupRefResult = await _businessAppRepository.RemoveAgentReferenceFromMessageCacheGroup(businessId, msgGroupId, businessAppAgent.Id, session);
                            if (!removeMsgGroupRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_MSG_GROUP_REF",
                                    "Failed to remove message group reference."
                                );
                            }
                        }

                        // Cache - Audios
                        foreach (var audioGroupId in businessAppAgent.Cache.Audios)
                        {
                            var removeAudioGroupRefResult = await _businessAppRepository.RemoveAgentReferenceFromAudioCacheGroup(businessId, audioGroupId, businessAppAgent.Id, session);
                            if (!removeAudioGroupRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_AUDIO_GROUP_REF",
                                    "Failed to remove audio group reference."
                                );
                            }
                        }

                        // Cache - Audio Auto Cache
                        if (businessAppAgent.Cache.AudioCacheSettings != null &&
                            businessAppAgent.Cache.AudioCacheSettings.AutoCacheAudioResponses &&
                            !string.IsNullOrEmpty(businessAppAgent.Cache.AudioCacheSettings.AutoCacheAudioResponseCacheGroupId))
                        {
                            var removeAudioGroupRefResult = await _businessAppRepository.RemoveAgentAutoCacheReferenceFromAudioCacheGroup(
                                businessId,
                                businessAppAgent.Cache.AudioCacheSettings.AutoCacheAudioResponseCacheGroupId,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeAudioGroupRefResult) {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_AUDIO_GROUP_REF",
                                    "Failed to remove audio group reference."
                                );
                            }
                        }

                        // Cache - Embeddings
                        foreach (var embGroupId in businessAppAgent.Cache.Embeddings)
                        {
                            var removeEmbeddingGroupRefResult = await _businessAppRepository.RemoveAgentReferenceFromEmbeddingCacheGroup(businessId, embGroupId, businessAppAgent.Id, session);
                            if (!removeEmbeddingGroupRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_EMB_GROUP_REF",
                                    "Failed to remove embedding group reference."
                                );
                            }
                        }

                        // Cache - Embedding Auto Cache
                        if (businessAppAgent.Cache.EmbeddingsCacheSettings != null &&
                            businessAppAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponses &&
                            !string.IsNullOrEmpty(businessAppAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponseCacheGroupId))
                        {
                            var removeEmbeddingGroupRefResult = await _businessAppRepository.RemoveAgentAutoCacheReferenceFromEmbeddingCacheGroup(
                                businessId,
                                businessAppAgent.Cache.EmbeddingsCacheSettings.AutoCacheEmbeddingResponseCacheGroupId,
                                businessAppAgent.Id,
                                session
                            );
                            if (!removeEmbeddingGroupRefResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "DeleteAgent:REMOVE_EMB_GROUP_REF",
                                    "Failed to remove embedding group reference."
                                );
                            }
                        }

                        // Delete DB
                        var deleteResult = await _businessAppRepository.DeleteAgent(businessId, businessAppAgent.Id, session);
                        if (!deleteResult)
                        {
                            return result.SetFailureResult(
                                "DeleteAgent:FAILED_DB_DELETE",
                                "Failed to delete agent from database."
                            );
                        }

                        await session.CommitTransactionAsync();
                        return result.SetSuccessResult();
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "DeleteAgent:EXCEPTION",
                            $"An error occurred: {ex.Message}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteAgent:EXCEPTION",
                    $"An error occurred: {ex.Message}"
                );
            }
        }
    }
}

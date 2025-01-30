using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraCore.Utilities.Audio;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace IqraInfrastructure.Services.Business
{
    public class BusinessAgentsManager
    {
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly BusinessAgentAudioRepository _businessAgentAudioRepository;

        private readonly AudioFileProcessor _audioProcessor;

        public BusinessAgentsManager(BusinessAppRepository businessAppRepository, BusinessRepository businessRepository, BusinessAgentAudioRepository businessAgentAudioRepository, AudioFileProcessor audioProcessor)
        {
            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
            _businessAgentAudioRepository = businessAgentAudioRepository;

            _audioProcessor = audioProcessor;
        }

        public async Task<FunctionReturnResult<BusinessAppAgent?>> AddOrUpdateAgent(long businessId, string postType, IFormCollection formData, string existingAgentId)
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
            var agent = new BusinessAppAgent();

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
                    agent.General.Emoji = emojiElement.GetString();
                }

                var nameValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    generalTabElement,
                    "name",
                    agent.General.Name
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
                    agent.General.Description
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
                    agent.Context.UseBranding = useBrandingElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useBranches", out var useBranchesElement))
                {
                    agent.Context.UseBranches = useBranchesElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useServices", out var useServicesElement))
                {
                    agent.Context.UseServices = useServicesElement.GetBoolean();
                }

                if (contextTabElement.TryGetProperty("useProducts", out var useProductsElement))
                {
                    agent.Context.UseProducts = useProductsElement.GetBoolean();
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
                    agent.Personality.Name
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
                    agent.Personality.Role
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
                    agent.Personality.Capabilities,
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
                    agent.Personality.Ethics,
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
                    agent.Personality.Tone,
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
                if (utterancesTabElement.TryGetProperty("openingType", out var openingTypeElement))
                {
                    agent.Utterances.OpeningType = openingTypeElement.GetString();
                }

                var greetingMessageValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(
                    businessLanguages,
                    utterancesTabElement,
                    "greetingMessage",
                    agent.Utterances.GreetingMessage
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
                    agent.Utterances.PhrasesBeforeReply
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
                result.Code = "AddOrUpdateAgent:8";
                result.Message = "Integrations section not found.";
                return result;
            }
            else
            {
                if (integrationsTabElement.TryGetProperty("STT", out var sttElement))
                {
                    try
                    {
                        agent.Integrations.STT = JsonSerializer.Deserialize<BusinessAppAgentIntegrationSTT>(sttElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:9";
                        result.Message = "Invalid STT integration data: " + ex.Message;
                        return result;
                    }
                }

                if (integrationsTabElement.TryGetProperty("LLM", out var llmElement))
                {
                    try
                    {
                        agent.Integrations.LLM = JsonSerializer.Deserialize<BusinessAppAgentIntegrationLLM>(llmElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:10";
                        result.Message = "Invalid LLM integration data: " + ex.Message;
                        return result;
                    }
                }

                if (integrationsTabElement.TryGetProperty("TTS", out var ttsElement))
                {
                    try
                    {
                        agent.Integrations.TTS = JsonSerializer.Deserialize<BusinessAppAgentIntegrationTTS>(ttsElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:11";
                        result.Message = "Invalid TTS integration data: " + ex.Message;
                        return result;
                    }
                }
            }

            // Cache Section
            if (!changesRootElement.TryGetProperty("cache", out var cacheTabElement))
            {
                result.Code = "AddOrUpdateAgent:12";
                result.Message = "Cache section not found.";
                return result;
            }
            else
            {
                if (cacheTabElement.TryGetProperty("messages", out var messagesElement))
                {
                    try
                    {
                        agent.Cache.Messages = JsonSerializer.Deserialize<List<BusinessAppAgentCacheMessage>>(messagesElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:13";
                        result.Message = "Invalid cache messages data: " + ex.Message;
                        return result;
                    }
                }

                if (cacheTabElement.TryGetProperty("audios", out var audiosElement))
                {
                    try
                    {
                        agent.Cache.Audios = JsonSerializer.Deserialize<List<BusinessAppAgentCacheAudio>>(audiosElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:14";
                        result.Message = "Invalid cache audios data: " + ex.Message;
                        return result;
                    }
                }

                if (cacheTabElement.TryGetProperty("autoCacheAudioSettings", out var autoCacheSettingsElement))
                {
                    try
                    {
                        agent.Cache.AutoCacheAudioSettings = JsonSerializer.Deserialize<BusinessAppAgentCacheAutoSettings>(autoCacheSettingsElement.GetRawText());
                    }
                    catch (Exception ex)
                    {
                        result.Code = "AddOrUpdateAgent:15";
                        result.Message = "Invalid auto cache settings data: " + ex.Message;
                        return result;
                    }
                }
            }

            // Handle Background Audio File
            if (formData.Files.Count > 0)
            {
                var backgroundAudio = formData.Files.GetFile("backgroundAudio");
                if (backgroundAudio != null)
                {
                    var validationResult = await _audioProcessor.ValidateAudioFile(backgroundAudio);
                    if (!validationResult.IsValid)
                    {
                        result.Code = "AddOrUpdateAgent:18";
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

                    agent.Settings.BackgroundAudioUrl = validationResult.Hash;
                }
            }

            if (postType == "new")
            {
                agent.Id = Guid.NewGuid().ToString();

                var addAgentResult = await _businessAppRepository.AddAgent(businessId, agent);
                if (!addAgentResult)
                {
                    result.Code = "AddOrUpdateAgent:19";
                    result.Message = "Failed to add business agent.";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                agent.Id = existingAgentId;

                var updateAgentResult = await _businessAppRepository.UpdateAgent(businessId, agent);
                if (!updateAgentResult)
                {
                    result.Code = "AddOrUpdateAgent:20";
                    result.Message = "Failed to update business agent.";
                    return result;
                }
            }

            result.Success = true;
            result.Data = agent;
            return result;
        }

        public async Task<bool> CheckAgentExists(long businessId, string existingAgentId)
        {
            var result = await _businessAppRepository.CheckAgentExists(businessId, existingAgentId);
            return result;
        }
    }
}

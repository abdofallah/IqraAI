using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraInfrastructure.Repositories.Languages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Languages
{
    public class LanguagesManager
    {
        private readonly ILogger<LanguagesManager> _logger;

        private readonly LanguagesRepository _languagesRepository;
        public LanguagesManager(ILogger<LanguagesManager> logger, LanguagesRepository languagesRepository)
        {
            _logger = logger;

            _languagesRepository = languagesRepository;
        }

        public async Task<FunctionReturnResult<LanguagesData?>> AddUpdateLanguage(IFormCollection formData, string postType, string languageCode, LanguagesData? oldLanguageData)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrEmpty(changesJsonString))
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:INVALID_PAYLOAD",
                    "Changes data is required."
                );
            }

            JsonDocument changesJsonElement;
            try
            {
                changesJsonElement = JsonDocument.Parse(changesJsonString.ToString());
            }
            catch
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:INVALID_JSON",
                    "Invalid changes data format."
                );
            }
            var root = changesJsonElement.RootElement;

            LanguagesData newLanguagesData = new LanguagesData
            {
                Id = languageCode,
                Prompts = oldLanguageData?.Prompts ?? new LanguagePromptsData()
            };

            if (!changesJsonElement.RootElement.TryGetProperty("name", out var languageNameElement))
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:MISSING_LANGUAGE_NAME",
                    "Language name is required."
                );
            }
            else
            {
                string? languageName = languageNameElement.GetString();
                if (string.IsNullOrEmpty(languageName))
                {
                    return result.SetFailureResult(
                        "AddUpdateLanguage:INVALID_LANGUAGE_NAME",
                        "Invalid language name."
                    );
                }
                newLanguagesData.Name = languageName;
            }

            if (!changesJsonElement.RootElement.TryGetProperty("localeName", out var languageLocaleNameElement))
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:MISSING_LANGUAGE_LOCALE_NAME",
                    "Language locale name is required."
                );
            }
            else
            {
                string? languageLocalceName = languageLocaleNameElement.GetString();
                if (string.IsNullOrEmpty(languageLocalceName))
                {
                    return result.SetFailureResult(
                        "AddUpdateLanguage:INVALID_LANGUAGE_LOCALE_NAME",
                        "Invalid language locale name."
                    );
                }
                newLanguagesData.LocaleName = languageLocalceName;
            }

            if (!changesJsonElement.RootElement.TryGetProperty("disabled", out var languageDisabledElement))
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:MISSING_LANGUAGE_DISABLED",
                    "Language disabled is required."
                );
            }
            else
            {
                bool? languageDisabled = languageDisabledElement.GetBoolean();
                if (languageDisabled == null)
                {
                    return result.SetFailureResult(
                        "AddUpdateLanguage:INVALID_LANGUAGE_DISABLED",
                        "Invalid language disabled."
                    );
                }

                if (languageDisabled == true)
                {
                    newLanguagesData.DisabledAt = DateTime.UtcNow;

                    if (oldLanguageData != null && oldLanguageData.DisabledAt != null)
                    {
                        newLanguagesData.DisabledAt = oldLanguageData.DisabledAt;
                    }
                }

                if (newLanguagesData.DisabledAt != null)
                {
                    if (!changesJsonElement.RootElement.TryGetProperty("publicDisabledReason", out var pubReasonEl))
                    {
                        return result.SetFailureResult(
                            "AddUpdateLanguage:MISSING_PUBLIC_DISABLED_REASON",
                            "Public disabled reason is required."
                        );
                    }
                    else
                    {
                        var pubReason = pubReasonEl.GetString();
                        if (string.IsNullOrEmpty(pubReason))
                        {
                            return result.SetFailureResult(
                                "AddUpdateLanguage:INVALID_PUBLIC_DISABLED_REASON",
                                "Invalid public disabled reason."
                            );
                        }
                        newLanguagesData.PublicDisabledReason = pubReason;
                    }

                    if (!changesJsonElement.RootElement.TryGetProperty("privateDisabledReason", out var privReasonEl))
                    {
                        return result.SetFailureResult(
                            "AddUpdateLanguage:MISSING_PRIVATE_DISABLED_REASON",
                            "Private disabled reason is required."
                        );
                    }
                    else
                    {
                        var privReason = privReasonEl.GetString();
                        if (string.IsNullOrEmpty(privReason))
                        {
                            return result.SetFailureResult(
                                "AddUpdateLanguage:INVALID_PRIVATE_DISABLED_REASON",
                                "Invalid private disabled reason."
                            );
                        }
                        newLanguagesData.PrivateDisabledReason = privReason;
                    }
                }
            }

            if (!root.TryGetProperty("prompts", out var promptsEl))
            {
                return result.SetFailureResult(
                    "AddUpdateLanguage:MISSING_PROMPTS",
                    "Prompts is required."
                );
            }
            else
            {
                // Helper to safely get string from JSON or fallback to existing
                string GetPrompt(string key, string existingVal) =>
                    promptsEl.TryGetProperty(key, out var val) ? (val.GetString() ?? "") : existingVal;

                var p = newLanguagesData.Prompts;
                var oldP = oldLanguageData?.Prompts ?? new LanguagePromptsData();

                // Conversation
                p.ConversationWarmupLLMPrompt = GetPrompt("conversationWarmupLLMPrompt", oldP.ConversationWarmupLLMPrompt);
                p.ConversationBasePrompt = GetPrompt("conversationBasePrompt", oldP.ConversationBasePrompt);
                p.FailedConversationBasePromptGenerationPrompt = GetPrompt("failedConversationBasePromptGenerationPrompt", oldP.FailedConversationBasePromptGenerationPrompt);

                // Verification
                p.TurnEndVerificationPrompt = GetPrompt("turnEndVerificationPrompt", oldP.TurnEndVerificationPrompt);
                p.InterruptionVerificationPrompt = GetPrompt("interruptionVerificationPrompt", oldP.InterruptionVerificationPrompt);
                p.VoicemailVerificationPrompt = GetPrompt("voicemailVerificationPrompt", oldP.VoicemailVerificationPrompt);

                // RAG
                p.RagQueryClassifierPrompt = GetPrompt("ragQueryClassifierPrompt", oldP.RagQueryClassifierPrompt);
                p.RagQueryRefinementPrompt = GetPrompt("ragQueryRefinementPrompt", oldP.RagQueryRefinementPrompt);

                // Post Analysis
                p.PostAnalaysisSummaryGenerationPrompt = GetPrompt("postAnalaysisSummaryGenerationPrompt", oldP.PostAnalaysisSummaryGenerationPrompt);
                p.PostAnalaysisSummaryGenerationPromptQuery = GetPrompt("postAnalaysisSummaryGenerationPromptQuery", oldP.PostAnalaysisSummaryGenerationPromptQuery);
                p.PostAnalaysisTagsClassificationPrompt = GetPrompt("postAnalaysisTagsClassificationPrompt", oldP.PostAnalaysisTagsClassificationPrompt);
                p.PostAnalaysisTagsClassificationPromptQuery = GetPrompt("postAnalaysisTagsClassificationPromptQuery", oldP.PostAnalaysisTagsClassificationPromptQuery);
                p.PostAnalaysisDataExtractionPrompt = GetPrompt("postAnalaysisDataExtractionPrompt", oldP.PostAnalaysisDataExtractionPrompt);
                p.PostAnalaysisDataExtractionPromptQuery = GetPrompt("postAnalaysisDataExtractionPromptQuery", oldP.PostAnalaysisDataExtractionPromptQuery);
            }

            if (newLanguagesData.DisabledAt == null)
            {
                var promptValidation = ValidatePromptsAreFilled(newLanguagesData.Prompts);
                if (!promptValidation.Success)
                {
                    return result.SetFailureResult(
                        $"AddUpdateLanguage:{promptValidation.Code}",
                        promptValidation.Message
                    );
                }
            }

            if (postType == "new")
            {
                // Force disable on creation just in case logic slipped, though we handle it above
                if (newLanguagesData.DisabledAt == null)
                {
                    newLanguagesData.DisabledAt = DateTime.UtcNow;
                }

                bool addResult = await _languagesRepository.AddNewLanguage(newLanguagesData);
                if (!addResult)
                {
                    return result.SetFailureResult(
                        "AddUpdateLanguage:ADD_FAILED",
                        "Language add failed."
                    );
                }
            }
            else if (postType == "edit")
            {
                bool replaceResult = await _languagesRepository.ReplaceLanguage(newLanguagesData);
                if (!replaceResult)
                {
                    return result.SetFailureResult(
                        "AddUpdateLanguage:UPDATE_FAILED",
                        "Language update failed."
                    );
                }
            }

            return result.SetSuccessResult(newLanguagesData);
        }
        private FunctionReturnResult ValidatePromptsAreFilled(LanguagePromptsData prompts)
        {
            var result = new FunctionReturnResult();

            // Conversation Prompts
            if (string.IsNullOrWhiteSpace(prompts.ConversationWarmupLLMPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_WARMUP_PROMPT",
                    "Conversation Warmup LLM Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.ConversationBasePrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_BASE_PROMPT",
                    "Conversation Base Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.FailedConversationBasePromptGenerationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_FAILED_GEN_PROMPT",
                    "Failed Conversation Base Prompt Generation Prompt is missing."
                );
            }

            // Verification Prompts
            if (string.IsNullOrWhiteSpace(prompts.TurnEndVerificationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_TURN_END_PROMPT",
                    "Turn End Verification Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.InterruptionVerificationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_INTERRUPTION_PROMPT",
                    "Interruption Verification Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.VoicemailVerificationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_VOICEMAIL_PROMPT",
                    "Voicemail Verification Prompt is missing."
                );
            }

            // RAG Prompts
            if (string.IsNullOrWhiteSpace(prompts.RagQueryClassifierPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_RAG_CLASSIFIER_PROMPT",
                    "RAG Query Classifier Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.RagQueryRefinementPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_RAG_REFINEMENT_PROMPT",
                    "RAG Query Refinement Prompt is missing."
                );
            }

            // Post Analysis - Summary
            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisSummaryGenerationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_SUMMARY_PROMPT",
                    "Post Analysis Summary Generation Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisSummaryGenerationPromptQuery))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_SUMMARY_QUERY_PROMPT",
                    "Post Analysis Summary Generation Prompt Query is missing."
                );
            }

            // Post Analysis - Tags
            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisTagsClassificationPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_TAGS_PROMPT",
                    "Post Analysis Tags Classification Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisTagsClassificationPromptQuery))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_TAGS_QUERY_PROMPT",
                    "Post Analysis Tags Classification Prompt Query is missing."
                );
            }

            // Post Analysis - Data Extraction
            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisDataExtractionPrompt))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_DATA_EXTRACT_PROMPT",
                    "Post Analysis Data Extraction Prompt is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(prompts.PostAnalaysisDataExtractionPromptQuery))
            {
                return result.SetFailureResult(
                    "ValidatePromptsAreFilled:MISSING_DATA_EXTRACT_QUERY_PROMPT",
                    "Post Analysis Data Extraction Prompt Query is missing."
                );
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult<LanguagesData?>> GetLanguageByCode(string languageCode, bool withPrompts = false)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            var getResult = await _languagesRepository.GetLanguageByCode(languageCode, withPrompts);
            if (getResult == null)
            {
                return result.SetFailureResult(
                    "GetLanguageByCode:NOT_FOUND",
                    "Language Not Found"
                );
            }

            return result.SetSuccessResult(getResult);
        }

        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguagesList(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            var getResult = await _languagesRepository.GetLanguagesList(page, pageSize);
            if (getResult == null)
            {
                return result.SetFailureResult(
                    "GetLanguagesList:NOT_FOUND",
                    "Languages Not Found"
                );
            }
            
            return result.SetSuccessResult(getResult);
        }

        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetAllLanguagesList(bool withPrompts = false)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            var getResult = await _languagesRepository.GetAllLanguagesList(withPrompts);
            if (getResult == null)
            {
                return result.SetFailureResult(
                    "GetAllLanguagesList:NOT_FOUND",
                    "Languages Not Found"
                );
            }

            return result.SetSuccessResult(getResult);
        }
    }
}

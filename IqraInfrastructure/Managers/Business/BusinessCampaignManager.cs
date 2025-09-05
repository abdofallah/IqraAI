using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessCampaignManager
    {
        private readonly BusinessManager _parentBusinessManager;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        public BusinessCampaignManager(
            BusinessManager businessManager,
            BusinessAppRepository businessAppRepository,
            BusinessRepository businessRepository,
            IntegrationConfigurationManager integrationConfigurationManager
        ) {
            _parentBusinessManager = businessManager;
            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
            _integrationConfigurationManager = integrationConfigurationManager;;
        }

        public async Task<FunctionReturnResult<BusinessAppCampaign?>> GetCampaignById(long businessId, string existingCampaignId)
        {
            var result = new FunctionReturnResult<BusinessAppCampaign?>();

            try
            {
                var data = await _businessAppRepository.GetBusinessCampaignById(businessId, existingCampaignId);
                if (data == null)
                {
                    return result.SetFailureResult(
                        "GetCampaignById:NOT_FOUND",
                        "Campaign not found."
                    );
                }

                return result.SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetCampaignById:EXCEPTION",
                    $"Error retrieving campaign: {ex.Message}"
                );
            }
        }
         
        public async Task<FunctionReturnResult<BusinessAppCampaign?>> AddOrUpdateCampaignAsync(long businessId, IFormCollection formData, string postType, BusinessAppCampaign? existingCampaignData)
        {
            var result = new FunctionReturnResult<BusinessAppCampaign?>();

            try
            {
                var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
                var businessNumbers = await _businessAppRepository.GetBusinessNumbers(businessId);

                if (!formData.TryGetValue("changes", out var changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateCampaignAsync:CHANGES_NOT_FOUND",
                        "Changes not found in form data."
                    );
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(changesJsonString))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateCampaignAsync:CHANGES_IS_REQUIRED",
                            "Changes is required."
                        );
                    }

                    JsonDocument? changes = null;
                    try
                    {
                        changes = JsonDocument.Parse(changesJsonString);
                    }
                    catch (Exception ex)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateCampaignAsync:CHANGES_PARSE_FAILED",
                            $"Unable to parse changes json string: {ex.Message}"
                        );
                    }

                    if (changes == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateCampaignAsync:CHANGES_PARSE_FAILED",
                            "Unable to parse changes json string."
                        );
                    }
                    else
                    {
                        var newBusinessAppCampaignData = new BusinessAppCampaign();

                        // General Tab
                        if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:GENERAL_TAB_NOT_FOUND",
                                "General tab not found."
                            );
                        }
                        else
                        {
                            if (!generalTabRootElement.TryGetProperty("emoji", out var generalEmojiProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:GENERAL_EMOJI_NOT_FOUND",
                                    "General emoji not found."
                                );
                            }
                            else
                            {
                                string? emoji = generalEmojiProperty.GetString();
                                if (string.IsNullOrWhiteSpace(emoji))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:GENERAL_EMOJI_IS_REQUIRED",
                                        "General emoji is required."
                                    );
                                }
                                newBusinessAppCampaignData.General.Emoji = emoji;
                            }

                            if (!generalTabRootElement.TryGetProperty("name", out var generalNameProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:GENERAL_NAME_NOT_FOUND",
                                    "General name not found."
                                );
                            }
                            else
                            {
                                string? name = generalNameProperty.GetString();
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:GENERAL_NAME_IS_REQUIRED",
                                        "General name is required."
                                    );
                                }
                                newBusinessAppCampaignData.General.Name = name;
                            }

                            if (!generalTabRootElement.TryGetProperty("description", out var generalDescriptionProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:GENERAL_DESCRIPTION_NOT_FOUND",
                                    "General description not found."
                                );
                            }
                            else
                            {
                                string? description = generalDescriptionProperty.GetString();
                                if (string.IsNullOrWhiteSpace(description))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:GENERAL_DESCRIPTION_IS_REQUIRED",
                                        "General description is required."
                                    );
                                }
                                newBusinessAppCampaignData.General.Description = description;
                            }
                        }

                        // Agent Tab
                        if (!changes.RootElement.TryGetProperty("agent", out var agentTabRootElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:AGENT_TAB_NOT_FOUND",
                                "Agent tab not found."
                            );
                        }
                        else
                        {
                            if (!agentTabRootElement.TryGetProperty("selectedAgentId", out var selectedAgentIdProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:AGENT_ID_NOT_FOUND",
                                    "Selected agent id not found."
                                );
                            }
                            else
                            {
                                string? selectedAgentId = selectedAgentIdProperty.GetString();
                                if (string.IsNullOrWhiteSpace(selectedAgentId))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_ID_IS_REQUIRED",
                                        "Selected agent id is required."
                                    );
                                }
                                var getBusinessAgent = await _businessAppRepository.GetAgentById(businessId, selectedAgentId);
                                if (getBusinessAgent == null)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_NOT_FOUND_IN_DB",
                                        "Selected agent not found."
                                    );
                                }
                                newBusinessAppCampaignData.Agent.SelectedAgentId = selectedAgentId;

                                if (!agentTabRootElement.TryGetProperty("openingScriptId", out var openingScriptIdProperty))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_SCRIPT_ID_NOT_FOUND",
                                        "Opening script id not found."
                                    );
                                }
                                else
                                {
                                    string? openingScriptId = openingScriptIdProperty.GetString();
                                    if (string.IsNullOrWhiteSpace(openingScriptId))
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:AGENT_SCRIPT_ID_IS_REQUIRED",
                                            "Opening script id is required."
                                        );
                                    }
                                    if (getBusinessAgent.Scripts.Find(x => x.Id == openingScriptId) == null)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:AGENT_SCRIPT_NOT_FOUND_IN_AGENT",
                                            "Opening script not found within selected agent."
                                        );
                                    }
                                    newBusinessAppCampaignData.Agent.OpeningScriptId = openingScriptId;
                                }
                            }

                            if (!agentTabRootElement.TryGetProperty("language", out var languageProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:AGENT_LANGUAGE_NOT_FOUND",
                                    "Language not found."
                                );
                            }
                            else
                            {
                                string? language = languageProperty.GetString();
                                if (string.IsNullOrWhiteSpace(language))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_LANGUAGE_IS_REQUIRED",
                                        "Language is required."
                                    );
                                }
                                if (!businessLanguages.Contains(language))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_LANGUAGE_NOT_ENABLED",
                                        $"Language {language} is not enabled for this business."
                                    );
                                }
                                newBusinessAppCampaignData.Agent.Language = language;
                            }

                            if (!agentTabRootElement.TryGetProperty("timezones", out var timezonesProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:AGENT_TIMEZONES_NOT_FOUND",
                                    "Timezones not found."
                                );
                            }
                            else
                            {
                                newBusinessAppCampaignData.Agent.Timezones = new List<string>();
                                foreach (var timezone in timezonesProperty.EnumerateArray())
                                {
                                    string? timezoneValue = timezone.GetString();
                                    if (string.IsNullOrWhiteSpace(timezoneValue))
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:AGENT_TIMEZONE_INVALID",
                                            "Invalid timezone value found in timezones list."
                                        );
                                    }
                                    if (!TimeZoneHelper.ValidateOffsetString(timezoneValue))
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:AGENT_TIMEZONE_VALIDATION_FAILED",
                                            $"Unable to validate timezone {timezoneValue}."
                                        );
                                    }
                                    newBusinessAppCampaignData.Agent.Timezones.Add(timezoneValue);
                                }
                            }

                            if (!agentTabRootElement.TryGetProperty("fromNumberInContext", out var fromNumberInContextProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:AGENT_FROM_NUMBER_CONTEXT_NOT_FOUND",
                                    "'From Number In Context' setting not found."
                                );
                            }
                            else
                            {
                                if (fromNumberInContextProperty.ValueKind != JsonValueKind.True && fromNumberInContextProperty.ValueKind != JsonValueKind.False)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_FROM_NUMBER_CONTEXT_INVALID",
                                        "'From Number In Context' setting is invalid."
                                    );
                                }

                                newBusinessAppCampaignData.Agent.FromNumberInContext = fromNumberInContextProperty.GetBoolean();
                            }

                            if (!agentTabRootElement.TryGetProperty("toNumberInContext", out var toNumberInContextProperty))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:AGENT_TO_NUMBER_CONTEXT_NOT_FOUND",
                                    "'To Number In Context' setting not found."
                                );
                            }
                            else
                            {
                                if (toNumberInContextProperty.ValueKind != JsonValueKind.True && toNumberInContextProperty.ValueKind != JsonValueKind.False)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:AGENT_TO_NUMBER_CONTEXT_INVALID",
                                        "'To Number In Context' setting is invalid."
                                    );
                                }

                                newBusinessAppCampaignData.Agent.ToNumberInContext = toNumberInContextProperty.GetBoolean();
                            }
                        }

                        // Numbers Tab
                        if (!changes.RootElement.TryGetProperty("numbers", out var numbersProperty))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:NUMBERS_TAB_NOT_FOUND",
                                "Numbers not found."
                            );
                        }
                        else
                        {
                            newBusinessAppCampaignData.Numbers = new List<string>();
                            foreach (var number in numbersProperty.EnumerateArray())
                            {
                                string? numberId = number.GetString();
                                if (string.IsNullOrWhiteSpace(numberId))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:NUMBER_ID_INVALID",
                                        "Invalid number id found in numbers list."
                                    );
                                }
                                var numberData = businessNumbers.Find(x => x.Id == numberId);
                                if (numberData == null)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:NUMBER_NOT_FOUND_IN_BUSINESS",
                                        "Number with id " + numberId + " not found in business numbers list."
                                    );
                                }
                                newBusinessAppCampaignData.Numbers.Add(numberId);
                            }
                        }

                        // Configuration Tab
                        if (!changes.RootElement.TryGetProperty("configuration", out var configTabRootElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:CONFIG_TAB_NOT_FOUND",
                                "Configuration tab not found."
                            );
                        }
                        else
                        {
                            // Retry on Decline
                            if (!configTabRootElement.TryGetProperty("retryOnDecline", out var retryDeclineElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_NOT_FOUND",
                                    "Retry on decline settings not found."
                                );
                            }
                            else
                            {
                                if (!retryDeclineElement.TryGetProperty("enabled", out var retryDeclineEnabledProp) || (retryDeclineEnabledProp.ValueKind != JsonValueKind.True && retryDeclineEnabledProp.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_ENABLED_INVALID",
                                        "Invalid 'enabled' value for retry on decline."
                                    );
                                }
                                else
                                {
                                    newBusinessAppCampaignData.Configuration.RetryOnDecline.Enabled = retryDeclineEnabledProp.GetBoolean();
                                    if (newBusinessAppCampaignData.Configuration.RetryOnDecline.Enabled)
                                    {
                                        if (!retryDeclineElement.TryGetProperty("count", out var retryCountProp)
                                            || !retryCountProp.TryGetInt32(out var retryCount) || retryCount < 1)
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_COUNT_INVALID",
                                                "Invalid retry count for decline."
                                            );
                                        }
                                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Count = retryCount;

                                        if (!retryDeclineElement.TryGetProperty("delay", out var delayProp)
                                            || !delayProp.TryGetInt32(out var delay) || delay < 1)
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_DELAY_INVALID",
                                                "Invalid delay for decline retry."
                                            );
                                        }
                                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Delay = delay;

                                        if (!retryDeclineElement.TryGetProperty("unit", out var unitProp)
                                            || !unitProp.TryGetInt32(out int unitEnumInt))
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_UNIT_INVALID",
                                                "Invalid unit for decline retry."
                                            );
                                        }
                                        if (!Enum.IsDefined(typeof(OutboundCallRetryDelayUnitType), unitEnumInt))
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_DECLINE_UNIT_ENUM_INVALID",
                                                "Invalid unit enum for decline retry."
                                            );
                                        }

                                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Unit = (OutboundCallRetryDelayUnitType)unitEnumInt;
                                    }
                                }
                            }

                            // Retry on Miss
                            if (!configTabRootElement.TryGetProperty("retryOnMiss", out var retryMissElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_NOT_FOUND",
                                    "Retry on miss settings not found."
                                );
                            }
                            else
                            {
                                if (!retryMissElement.TryGetProperty("enabled", out var retryMissEnabledProp)
                                    || (retryMissEnabledProp.ValueKind != JsonValueKind.True && retryMissEnabledProp.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_ENABLED_INVALID",
                                        "Invalid 'enabled' value for retry on miss."
                                    );
                                }
                                else
                                {
                                    newBusinessAppCampaignData.Configuration.RetryOnMiss.Enabled = retryMissEnabledProp.GetBoolean();
                                    if (newBusinessAppCampaignData.Configuration.RetryOnMiss.Enabled)
                                    {
                                        if (!retryMissElement.TryGetProperty("count", out var retryCountProp)
                                            || !retryCountProp.TryGetInt32(out var retryCount) || retryCount < 1)
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_COUNT_INVALID",
                                                "Invalid retry count for miss."
                                            );
                                        }
                                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Count = retryCount;

                                        if (!retryMissElement.TryGetProperty("delay", out var delayProp)
                                            || !delayProp.TryGetInt32(out var delay) || delay < 1)
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_DELAY_INVALID",
                                                "Invalid delay for miss retry."
                                            );
                                        }
                                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Delay = delay;

                                        if (!retryMissElement.TryGetProperty("unit", out var unitProp)
                                            || !unitProp.TryGetInt32(out var unit))
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_UNIT_INVALID",
                                                "Invalid unit for miss retry."
                                            );
                                        }
                                        if (!Enum.IsDefined(typeof(OutboundCallRetryDelayUnitType), unit))
                                        {
                                            return result.SetFailureResult(
                                                "AddOrUpdateCampaignAsync:CONFIG_RETRY_MISS_UNIT_ENUM_INVALID",
                                                "Invalid unit enum for miss retry."
                                            );
                                        }

                                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Unit = (OutboundCallRetryDelayUnitType)unit;
                                    }
                                }
                            }

                            // Timeouts
                            if (!configTabRootElement.TryGetProperty("timeouts", out var timeoutsElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:CONFIG_TIMEOUTS_NOT_FOUND",
                                    "Timeouts settings not found."
                                );
                            }
                            else
                            {
                                // pickup delay
                                if (!timeoutsElement.TryGetProperty("pickupDelayMS", out var pickupDelayProp)
                                    || !pickupDelayProp.TryGetInt32(out var pickupDelay) || pickupDelay < 0)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_PICKUP_DELAY_INVALID",
                                        "Invalid pickup delay value."
                                    );
                                }
                                newBusinessAppCampaignData.Configuration.Timeouts.PickupDelayMS = pickupDelay;

                                if (!timeoutsElement.TryGetProperty("notifyOnSilenceMS", out var notifySilenceProp)
                                || !notifySilenceProp.TryGetInt32(out var notifySilence) || notifySilence < 0)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_NOTIFY_SILENCE_INVALID",
                                        "Invalid notify on silence value."
                                    );
                                }
                                newBusinessAppCampaignData.Configuration.Timeouts.NotifyOnSilenceMS = notifySilence;

                                if (!timeoutsElement.TryGetProperty("endOnSilenceMS", out var endSilenceProp)
                                    || !endSilenceProp.TryGetInt32(out var endSilence) || endSilence < 0)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_END_SILENCE_INVALID",
                                        "Invalid end call on silence value."
                                    );
                                }
                                newBusinessAppCampaignData.Configuration.Timeouts.EndOnSilenceMS = endSilence;

                                if (!timeoutsElement.TryGetProperty("maxCallTimeS", out var maxCallTimeProp)
                                    || !maxCallTimeProp.TryGetInt32(out var maxCallTime) || maxCallTime < 0)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:CONFIG_MAX_CALL_TIME_INVALID",
                                        "Invalid max call time value."
                                    );
                                }
                                newBusinessAppCampaignData.Configuration.Timeouts.MaxCallTimeS = maxCallTime;
                            }
                        }

                        // Voicemail Tab
                        if (!changes.RootElement.TryGetProperty("voicemailDetection", out var voicemailElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:VOICEMAIL_SECTION_MISSING",
                                "Voicemail section 'voicemailDetection' not found."
                            );
                        }
                        else
                        {
                            var voicemailData = new BusinessAppCampaignVoicemailDetection();

                            if (!voicemailElement.TryGetProperty("isEnabled", out var isEnabledElement)
                                || (isEnabledElement.ValueKind != JsonValueKind.True && isEnabledElement.ValueKind != JsonValueKind.False))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:VOICEMAIL_ISENABLED_INVALID",
                                    "Voicemail isEnabled parameter is missing or invalid."
                                );
                            }
                            voicemailData.IsEnabled = isEnabledElement.GetBoolean();

                            if (voicemailData.IsEnabled)
                            {
                                #region Voicemail Property Checks
                                if (!voicemailElement.TryGetProperty("initialCheckDelayMS", out var initialCheckDelayMSElement)
                                    || initialCheckDelayMSElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_INITIALCHECKDELAY_INVALID",
                                        "Voicemail initialCheckDelayMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.InitialCheckDelayMS = initialCheckDelayMSElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("mlCheckDurationMS", out var mlCheckDurationMSElement)
                                    || mlCheckDurationMSElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_MLCHECKDURATION_INVALID",
                                        "Voicemail mlCheckDurationMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.MLCheckDurationMS = mlCheckDurationMSElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("maxMLCheckTries", out var maxMLCheckTriesElement)
                                    || maxMLCheckTriesElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_MAXMLTRIES_INVALID",
                                        "Voicemail maxMLCheckTries parameter is missing or invalid."
                                        );
                                }
                                voicemailData.MaxMLCheckTries = maxMLCheckTriesElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("voiceMailMessageVADSilenceThresholdMS", out var vadSilenceThresholdElement)
                                    || vadSilenceThresholdElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_VADSILENCE_INVALID",
                                        "Voicemail voiceMailMessageVADSilenceThresholdMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.VoiceMailMessageVADSilenceThresholdMS = vadSilenceThresholdElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("voiceMailMessageVADMaxSpeechDurationMS", out var vadMaxSpeechDurationElement)
                                    || vadMaxSpeechDurationElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_VADMAXSPEECH_INVALID",
                                        "Voicemail voiceMailMessageVADMaxSpeechDurationMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.VoiceMailMessageVADMaxSpeechDurationMS = vadMaxSpeechDurationElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterXMlCheckSuccess", out var stopOnMlElement)
                                    || (stopOnMlElement.ValueKind != JsonValueKind.True && stopOnMlElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_STOPONML_INVALID",
                                        "Voicemail stopSpeakingAgentAfterXMlCheckSuccess parameter is missing or invalid."
                                        );
                                }
                                voicemailData.StopSpeakingAgentAfterXMlCheckSuccess = stopOnMlElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterVadSilence", out var stopOnVadElement)
                                    || (stopOnVadElement.ValueKind != JsonValueKind.True && stopOnVadElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_STOPONVAD_INVALID",
                                        "Voicemail stopSpeakingAgentAfterVadSilence parameter is missing or invalid."
                                        );
                                }
                                voicemailData.StopSpeakingAgentAfterVadSilence = stopOnVadElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterLLMConfirm", out var stopOnLlmElement)
                                    || (stopOnLlmElement.ValueKind != JsonValueKind.True && stopOnLlmElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_STOPONLLM_INVALID",
                                        "Voicemail stopSpeakingAgentAfterLLMConfirm parameter is missing or invalid."
                                        );
                                }
                                voicemailData.StopSpeakingAgentAfterLLMConfirm = stopOnLlmElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("stopSpeakingAgentDelayAfterMatchMS", out var stopDelayElement)
                                    || stopDelayElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_STOPDELAY_INVALID",
                                        "Voicemail stopSpeakingAgentDelayAfterMatchMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.StopSpeakingAgentDelayAfterMatchMS = stopDelayElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterXMLCheckSuccess", out var endOnMlElement)
                                    || (endOnMlElement.ValueKind != JsonValueKind.True && endOnMlElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDONML_INVALID",
                                        "Voicemail endOrLeaveMessageAfterXMLCheckSuccess parameter is missing or invalid."
                                        );
                                }
                                voicemailData.EndOrLeaveMessageAfterXMLCheckSuccess = endOnMlElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterVadSilence", out var endOnVadElement)
                                    || (endOnVadElement.ValueKind != JsonValueKind.True && endOnVadElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDONVAD_INVALID",
                                        "Voicemail endOrLeaveMessageAfterVadSilence parameter is missing or invalid."
                                        );
                                }
                                voicemailData.EndOrLeaveMessageAfterVadSilence = endOnVadElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterLLMConfirm", out var endOnLlmElement)
                                    || (endOnLlmElement.ValueKind != JsonValueKind.True && endOnLlmElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDONLLM_INVALID",
                                        "Voicemail endOrLeaveMessageAfterLLMConfirm parameter is missing or invalid."
                                        );
                                }
                                voicemailData.EndOrLeaveMessageAfterLLMConfirm = endOnLlmElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("endOrLeaveMessageDelayAfterMatchMS", out var endLeaveDelayElement)
                                    || endLeaveDelayElement.ValueKind != JsonValueKind.Number)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDDELAY_INVALID",
                                        "Voicemail endOrLeaveMessageDelayAfterMatchMS parameter is missing or invalid."
                                        );
                                }
                                voicemailData.EndOrLeaveMessageDelayAfterMatchMS = endLeaveDelayElement.GetInt32();

                                if (!voicemailElement.TryGetProperty("endCallOnDetect", out var endCallElement)
                                    || (endCallElement.ValueKind != JsonValueKind.True && endCallElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDCALL_INVALID",
                                        "Voicemail endCallOnDetect parameter is missing or invalid."
                                        );
                                }
                                voicemailData.EndCallOnDetect = endCallElement.GetBoolean();

                                if (!voicemailElement.TryGetProperty("leaveMessageOnDetect", out var leaveMessageElement)
                                    || (leaveMessageElement.ValueKind != JsonValueKind.True && leaveMessageElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_LEAVEMESSAGE_INVALID",
                                        "Voicemail leaveMessageOnDetect parameter is missing or invalid."
                                        );
                                }
                                voicemailData.LeaveMessageOnDetect = leaveMessageElement.GetBoolean();

                                if (voicemailData.EndCallOnDetect && voicemailData.LeaveMessageOnDetect)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ENDCALLANDLEAVEMESSAGE_INVALID",
                                        "Voicemail endCallOnDetect and leaveMessageOnDetect cannot be true at the same time."
                                        );
                                }

                                if (voicemailData.LeaveMessageOnDetect)
                                {
                                    voicemailData.MessageToLeave = new Dictionary<string, string>();
                                    var messageToLeaveValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(businessLanguages, voicemailElement, "messageToLeave", voicemailData.MessageToLeave);
                                    if (!messageToLeaveValidationResult.Success)
                                    {
                                        return result.SetFailureResult("AddOrUpdateCampaignAsync:" + messageToLeaveValidationResult.Code, messageToLeaveValidationResult.Message);
                                    }

                                    if (!voicemailElement.TryGetProperty("waitXMSAfterLeavingMessageToEndCall", out var waitAfterMessageElement)
                                        || waitAfterMessageElement.ValueKind != JsonValueKind.Number)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:VOICEMAIL_WAITAFTERMESSAGE_INVALID",
                                            "Voicemail waitXMSAfterLeavingMessageToEndCall parameter is missing or invalid."
                                            );
                                    }
                                    voicemailData.WaitXMSAfterLeavingMessageToEndCall = waitAfterMessageElement.GetInt32();
                                }

                                if (!voicemailElement.TryGetProperty("onVoiceMailMessageDetectVerifySTTAndLLM", out var advancedVerificationElement)
                                    || (advancedVerificationElement.ValueKind != JsonValueKind.True && advancedVerificationElement.ValueKind != JsonValueKind.False))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_ADVANCEDVERIFICATION_INVALID",
                                        "Voicemail onVoiceMailMessageDetectVerifySTTAndLLM parameter is missing or invalid."
                                        );
                                }
                                voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM = advancedVerificationElement.GetBoolean();

                                if ((voicemailData.StopSpeakingAgentAfterLLMConfirm || voicemailData.EndOrLeaveMessageAfterLLMConfirm)
                                    && !voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateCampaignAsync:VOICEMAIL_LLMTRIGGER_MISMATCH",
                                        "An LLM Confirmation trigger is enabled, but Advanced Verification is disabled."
                                        );
                                }

                                if (voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM)
                                {
                                    if (!voicemailElement.TryGetProperty("transcribeVoiceMessageSTT", out var sttIntegrationElement)
                                        || sttIntegrationElement.ValueKind == JsonValueKind.Null)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:VOICEMAIL_STT_INTEGRATION_MISSING",
                                            "STT integration for voicemail advanced verification is required but not provided."
                                            );
                                    }
                                    var sttValidationResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(businessId, sttIntegrationElement, "STT");
                                    if (!sttValidationResult.Success || sttValidationResult.Data == null)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:" + sttValidationResult.Code,
                                            "Voicemail STT Integration failed: " + sttValidationResult.Message
                                            );
                                    }
                                    voicemailData.TranscribeVoiceMessageSTT = sttValidationResult.Data;

                                    if (!voicemailElement.TryGetProperty("verifyVoiceMessageLLM", out var llmIntegrationElement)
                                        || llmIntegrationElement.ValueKind == JsonValueKind.Null)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:VOICEMAIL_LLM_INTEGRATION_MISSING",
                                            "LLM integration for voicemail advanced verification is required but not provided."
                                            );
                                    }
                                    var llmValidationResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(businessId, llmIntegrationElement, "LLM");
                                    if (!llmValidationResult.Success || llmValidationResult.Data == null)
                                    {
                                        return result.SetFailureResult(
                                            "AddOrUpdateCampaignAsync:" + llmValidationResult.Code,
                                            "Voicemail LLM Integration failed: " + llmValidationResult.Message
                                            );
                                    }
                                    voicemailData.VerifyVoiceMessageLLM = llmValidationResult.Data;
                                }
                                #endregion
                            }

                            newBusinessAppCampaignData.VoicemailDetection = voicemailData;
                        }

                        // Actions Tab
                        if (!changes.RootElement.TryGetProperty("actions", out var actionsTabRootElement))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateCampaignAsync:ACTIONS_TAB_NOT_FOUND",
                                "Actions tab not found."
                            );
                        }
                        else
                        {
                            if (!actionsTabRootElement.TryGetProperty("answeredTool", out var answeredToolElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:ACTIONS_ANSWERED_TOOL_NOT_FOUND",
                                    "Answered tool not found."
                                );
                            }
                            var answeredToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], answeredToolElement, "Answered");
                            if (!answeredToolValidationResult.Success)
                            {
                                return result.SetFailureResult("AddOrUpdateCampaignAsync:" + answeredToolValidationResult.Code, answeredToolValidationResult.Message);
                            }
                            newBusinessAppCampaignData.Actions.AnsweredTool = answeredToolValidationResult.Data;

                            if (!actionsTabRootElement.TryGetProperty("declinedTool", out var declinedToolElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:ACTIONS_DECLINED_TOOL_NOT_FOUND",
                                    "Declined tool not found."
                                );
                            }
                            var declinedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], declinedToolElement, "Declined");
                            if (!declinedToolValidationResult.Success)
                            {
                                return result.SetFailureResult("AddOrUpdateCampaignAsync:" + declinedToolValidationResult.Code, declinedToolValidationResult.Message);
                            }
                            newBusinessAppCampaignData.Actions.DeclinedTool = declinedToolValidationResult.Data;

                            if (!actionsTabRootElement.TryGetProperty("missedTool", out var missedToolElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:ACTIONS_MISSED_TOOL_NOT_FOUND",
                                    "No Missed tool not found."
                                );
                            }
                            var missedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], missedToolElement, "Missed");
                            if (!missedToolValidationResult.Success)
                            {
                                return result.SetFailureResult("AddOrUpdateCampaignAsync:" + missedToolValidationResult.Code, missedToolValidationResult.Message);
                            }
                            newBusinessAppCampaignData.Actions.MissedTool = missedToolValidationResult.Data;

                            if (!actionsTabRootElement.TryGetProperty("endedTool", out var endedToolElement))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:ACTIONS_ENDED_TOOL_NOT_FOUND",
                                    "Ended tool not found."
                                );
                            }
                            var endedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], endedToolElement, "Ended");
                            if (!endedToolValidationResult.Success)
                            {
                                return result.SetFailureResult("AddOrUpdateCampaignAsync:" + endedToolValidationResult.Code, endedToolValidationResult.Message);
                            }
                            newBusinessAppCampaignData.Actions.EndedTool = endedToolValidationResult.Data;
                        }

                        // Save or Update in Database
                        if (postType == "new")
                        {
                            newBusinessAppCampaignData.Id = Guid.NewGuid().ToString();
                            var addResult = await _businessAppRepository.AddBusinessAppCampaign(businessId, newBusinessAppCampaignData);
                            if (!addResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:DB_ADD_FAILED",
                                    "Failed to add business app campaign."
                                );
                            }
                        }
                        else
                        {
                            newBusinessAppCampaignData.Id = existingCampaignData.Id;
                            var updateResult = await _businessAppRepository.UpdateBusinessAppCampaign(businessId, newBusinessAppCampaignData);
                            if (!updateResult)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateCampaignAsync:DB_UPDATE_FAILED",
                                    "Failed to update business app campaign."
                                );
                            }
                        }

                        return result.SetSuccessResult(newBusinessAppCampaignData);
                    }
                }
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddOrUpdateCampaignAsync:EXCEPTION",
                    $"Error adding or updating campaign: {ex.Message}"
                );
            }
        }

        private async Task<FunctionReturnResult<BusinessAppCampaignActionConfig>> ValidateBusinessCampaignActionData(long businessId, string businessDefaultLanguage, JsonElement actionToolElement, string actionType)
        {
            var result = new FunctionReturnResult<BusinessAppCampaignActionConfig>();          
            var resultData = new BusinessAppCampaignActionConfig();

            if (!actionToolElement.TryGetProperty("toolId", out var toolIdProperty))
            {
                throw new Exception($"{actionType} selected tool id not found.");
            }

            string? toolId = toolIdProperty.GetString();
            if (toolId == null)
            {
                return result.SetSuccessResult(resultData);
            }
            var selectedToolData = await _businessAppRepository.GetBusinessAppTool(businessId, toolId);
            if (selectedToolData == null)
            {
                return result.SetFailureResult(
                    "ValidateBusinessCampaignActionData:TOOL_NOT_FOUND",
                    $"{actionType} tool not found in business."
                );
            }
            resultData.ToolId = toolId;
            resultData.Arguments = new Dictionary<string, object>();

            if (!actionToolElement.TryGetProperty("arguments", out var argumentsProperty))
            {
                if (selectedToolData.Configuration.InputSchemea.Any(arg => arg.IsRequired))
                {
                    return result.SetFailureResult(
                        "ValidateBusinessCampaignActionData:ARGS_MISSING_BUT_REQUIRED",
                        $"{actionType} tool arguments not found, but required arguments exist."
                    );
                }
            }

            if (argumentsProperty.ValueKind == JsonValueKind.Object)
            {
                foreach (var toolInputArgument in selectedToolData.Configuration.InputSchemea)
                {
                    bool foundProperty = argumentsProperty.TryGetProperty(toolInputArgument.Id, out var argumentValueProperty);

                    if (!foundProperty && toolInputArgument.IsRequired)
                    {
                        return result.SetFailureResult(
                            "ValidateBusinessCampaignActionData:REQUIRED_ARG_MISSING",
                            $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} not found but is required."
                        );
                    }
                    else if (foundProperty)
                    {
                        if (toolInputArgument.IsArray)
                        {
                            if (argumentValueProperty.ValueKind != JsonValueKind.Array)
                            {
                                return result.SetFailureResult(
                                    "ValidateBusinessCampaignActionData:ARG_NOT_ARRAY",
                                    $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} should be an array."
                                );
                            }

                            var arrayValues = new List<object>();
                            foreach (var arrayElement in argumentValueProperty.EnumerateArray())
                            {
                                var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, arrayElement, toolInputArgument, actionType);
                                if (!validationResult.Success)
                                {
                                    return result.SetFailureResult(
                                        $"ValidateBusinessCampaignActionData:{validationResult.Code}",
                                        validationResult.Message
                                        );
                                }
                                arrayValues.Add(validationResult.Data);
                            }

                            if (toolInputArgument.IsRequired && arrayValues.Count == 0)
                            {
                                return result.SetFailureResult(
                                    "ValidateBusinessCampaignActionData:REQUIRED_ARRAY_EMPTY",
                                    $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} array cannot be empty as it is required."
                                );
                            }
                            resultData.Arguments.Add(toolInputArgument.Id, arrayValues);
                        }
                        else
                        {
                            var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, argumentValueProperty, toolInputArgument, actionType);
                            if (!validationResult.Success)
                            {
                                return result.SetFailureResult(
                                    $"ValidateBusinessCampaignActionData:{validationResult.Code}",
                                    validationResult.Message
                                );
                            }
                            resultData.Arguments.Add(toolInputArgument.Id, validationResult.Data);
                        }
                    }
                }
            }

            return result.SetSuccessResult(resultData);
        }
    }
}

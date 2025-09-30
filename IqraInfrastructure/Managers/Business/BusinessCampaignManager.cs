using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Campaign;
using IqraCore.Entities.Helper.Call.Outbound;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using PhoneNumbers;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessCampaignManager
    {
        private readonly BusinessManager _parentBusinessManager;
        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        private readonly static List<CustomVariableInputTemplateVariableDefinition> TelephonyCampaginPostAnalysisContextVariableArguementsList = new List<CustomVariableInputTemplateVariableDefinition>()
        {
            // --- Call Queue Data ---
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_id", Name = "Call Queue Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_created_at", Name = "Call Queue Created At", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_enqueued_at", Name = "Call Queue Enqueued At", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_processing_started_at", Name = "Call Queue Processing Started At", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_completed_at", Name = "Call Queue Completed At", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_status", Name = "Call Queue Status", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_session_id", Name = "Call Queue Session Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_campaign_id", Name = "Call Queue Campaign Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_calling_number_id", Name = "Call Queue Calling Number Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_calling_number_provider", Name = "Call Queue Calling Number Provider", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_provider_call_id", Name = "Call Queue Provider Call Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_recipient_number", Name = "Call Queue Recipient Number", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_scheduled_for_date_time", Name = "Call Queue Scheduled For", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_dynamic_variables", Name = "Call Queue Dynamic Variables", Type = VariableType.Object },
            new CustomVariableInputTemplateVariableDefinition { Id = "call_queue_metadata", Name = "Call Queue Metadata", Type = VariableType.Object },

            // --- Conversation Data ---
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_id", Name = "Conversation Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_start_time", Name = "Conversation Start Time", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_end_type", Name = "Conversation End Type", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_end_time", Name = "Conversation End Time", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_turns", Name = "Conversation Turns", Type = VariableType.Object },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_turns_simplified", Name = "Conversation Turns Simplified", Type = VariableType.String },
        };
        private readonly static List<CustomVariableInputTemplateVariableDefinition> WebCampaginPostAnalysisContextVariableArguementsList = new List<CustomVariableInputTemplateVariableDefinition>()
        {
            // --- Web Session Data ---
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_id", Name = "Web Session Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_created_at", Name = "Web Session Created At", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_status", Name = "Web Session Status", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_web_campaign_id", Name = "Web Session Web Campaign Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_client_identifier", Name = "Web Session Client Identifier", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_dynamic_variables", Name = "Web Session Dynamic Variables", Type = VariableType.Object },
            new CustomVariableInputTemplateVariableDefinition { Id = "web_session_metadata", Name = "Web Session Metadata", Type = VariableType.Object },

            // --- Conversation Data ---
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_id", Name = "Conversation Id", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_start_time", Name = "Conversation Start Time", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_end_type", Name = "Conversation End Type", Type = VariableType.String },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_end_time", Name = "Conversation End Time", Type = VariableType.Datetime },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_turns", Name = "Conversation Turns", Type = VariableType.Object },
            new CustomVariableInputTemplateVariableDefinition { Id = "conversation_turns_simplified", Name = "Conversation Turns Simplified", Type = VariableType.String }, 
        };

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

        /**
         * 
         * Telephony Campaign
         * 
        **/
        public async Task<FunctionReturnResult<BusinessAppTelephonyCampaign?>> GetTelephonyCampaignById(long businessId, string existingTelephonyCampaignId)
        {
            var result = new FunctionReturnResult<BusinessAppTelephonyCampaign?>();

            try
            {
                var data = await _businessAppRepository.GetBusinessTelephonyCampaignById(businessId, existingTelephonyCampaignId);
                if (data == null)
                {
                    return result.SetFailureResult(
                        "GetTelephonyCampaignById:NOT_FOUND",
                        "Campaign not found."
                    );
                }

                return result.SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignById:EXCEPTION",
                    $"Error retrieving campaign: {ex.Message}"
                );
            }
        }
        public async Task<FunctionReturnResult<BusinessAppTelephonyCampaign?>> AddOrUpdateTelephonyCampaignAsync(long businessId, IFormCollection formData, string postType, BusinessAppTelephonyCampaign? existingCampaignData)
        {
            var result = new FunctionReturnResult<BusinessAppTelephonyCampaign?>();

            try
            {
                var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
                var businessNumbers = await _businessAppRepository.GetBusinessNumbers(businessId);

                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrWhiteSpace(changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:CHANGES_NOT_FOUND",
                        "Changes not found or is empty in form data."
                    );
                }

                JsonDocument changes;
                try
                {
                    changes = JsonDocument.Parse(changesJsonString!);
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:CHANGES_PARSE_FAILED",
                        $"Unable to parse changes json string: {ex.Message}"
                    );
                }

                var newBusinessAppCampaignData = new BusinessAppTelephonyCampaign();

                // General Tab
                if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:GENERAL_TAB_NOT_FOUND",
                        "General tab not found."
                    );
                }
                else
                {
                    if (!generalTabRootElement.TryGetProperty("emoji", out var generalEmojiProperty) || string.IsNullOrWhiteSpace(generalEmojiProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:GENERAL_EMOJI_IS_REQUIRED",
                            "General emoji is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Emoji = generalEmojiProperty.GetString()!;

                    if (!generalTabRootElement.TryGetProperty("name", out var generalNameProperty) || string.IsNullOrWhiteSpace(generalNameProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:GENERAL_NAME_IS_REQUIRED",
                            "General name is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Name = generalNameProperty.GetString()!;

                    if (!generalTabRootElement.TryGetProperty("description", out var generalDescriptionProperty) || string.IsNullOrWhiteSpace(generalDescriptionProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:GENERAL_DESCRIPTION_IS_REQUIRED",
                            "General description is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Description = generalDescriptionProperty.GetString()!;
                }

                // Agent Tab
                if (!changes.RootElement.TryGetProperty("agent", out var agentTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:AGENT_TAB_NOT_FOUND",
                        "Agent tab not found."
                    );
                }
                else
                {
                    if (!agentTabRootElement.TryGetProperty("selectedAgentId", out var selectedAgentIdProperty) || string.IsNullOrWhiteSpace(selectedAgentIdProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_ID_IS_REQUIRED",
                            "Selected agent id is required."
                        );
                    }
                    var selectedAgentId = selectedAgentIdProperty.GetString()!;
                    var getBusinessAgent = await _businessAppRepository.GetAgentById(businessId, selectedAgentId);
                    if (getBusinessAgent == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_NOT_FOUND_IN_DB",
                            "Selected agent not found."
                        );
                    }
                    newBusinessAppCampaignData.Agent.SelectedAgentId = selectedAgentId;

                    if (!agentTabRootElement.TryGetProperty("openingScriptId", out var openingScriptIdProperty) || string.IsNullOrWhiteSpace(openingScriptIdProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_SCRIPT_ID_IS_REQUIRED",
                            "Opening script id is required."
                        );
                    }
                    var openingScriptId = openingScriptIdProperty.GetString()!;
                    if (getBusinessAgent.Scripts.Find(x => x.Id == openingScriptId) == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_SCRIPT_NOT_FOUND_IN_AGENT",
                            "Opening script not found within selected agent."
                        );
                    }
                    newBusinessAppCampaignData.Agent.OpeningScriptId = openingScriptId;

                    if (!agentTabRootElement.TryGetProperty("language", out var languageProperty) || string.IsNullOrWhiteSpace(languageProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_LANGUAGE_IS_REQUIRED",
                            "Language is required."
                        );
                    }
                    var language = languageProperty.GetString()!;
                    if (!businessLanguages.Contains(language))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_LANGUAGE_NOT_ENABLED",
                            $"Language {language} is not enabled for this business."
                        );
                    }
                    newBusinessAppCampaignData.Agent.Language = language;

                    if (!agentTabRootElement.TryGetProperty("timezones", out var timezonesProperty) || timezonesProperty.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_TIMEZONES_NOT_FOUND",
                            "Timezones not found or invalid."
                        );
                    }
                    foreach (var timezone in timezonesProperty.EnumerateArray())
                    {
                        string? timezoneValue = timezone.GetString();
                        if (string.IsNullOrWhiteSpace(timezoneValue) || !TimeZoneHelper.ValidateOffsetString(timezoneValue))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:AGENT_TIMEZONE_VALIDATION_FAILED",
                                $"Unable to validate timezone {timezoneValue}."
                            );
                        }
                        newBusinessAppCampaignData.Agent.Timezones.Add(timezoneValue);
                    }

                    if (!agentTabRootElement.TryGetProperty("fromNumberInContext", out var fromNumberInContextProperty)
                        || (fromNumberInContextProperty.ValueKind != JsonValueKind.True && fromNumberInContextProperty.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_FROM_NUMBER_CONTEXT_INVALID",
                            "'From Number In Context' setting is invalid."
                        );
                    }
                    newBusinessAppCampaignData.Agent.FromNumberInContext = fromNumberInContextProperty.GetBoolean();

                    if (!agentTabRootElement.TryGetProperty("toNumberInContext", out var toNumberInContextProperty)
                        || (toNumberInContextProperty.ValueKind != JsonValueKind.True && toNumberInContextProperty.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:AGENT_TO_NUMBER_CONTEXT_INVALID",
                            "'To Number In Context' setting is invalid."
                        );
                    }
                    newBusinessAppCampaignData.Agent.ToNumberInContext = toNumberInContextProperty.GetBoolean();
                }

                // Configuration Tab
                if (!changes.RootElement.TryGetProperty("configuration", out var configTabRootElement))
                {
                    return result.SetFailureResult("AddOrUpdateTelephonyCampaignAsync:CONFIG_TAB_NOT_FOUND", "Configuration tab not found.");
                }
                else
                {
                    // Retry on Decline
                    if (!configTabRootElement.TryGetProperty("retryOnDecline", out var retryDeclineElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_DECLINE_NOT_FOUND",
                            "Retry on decline settings not found."
                        );
                    }
                    if (!retryDeclineElement.TryGetProperty("enabled", out var retryDeclineEnabledProp) || (retryDeclineEnabledProp.ValueKind != JsonValueKind.True && retryDeclineEnabledProp.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_DECLINE_ENABLED_INVALID",
                            "Invalid 'enabled' value for retry on decline."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.RetryOnDecline.Enabled = retryDeclineEnabledProp.GetBoolean();
                    if (newBusinessAppCampaignData.Configuration.RetryOnDecline.Enabled)
                    {
                        if (!retryDeclineElement.TryGetProperty("count", out var retryCountProp) || !retryCountProp.TryGetInt32(out var retryCount) || retryCount < 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_DECLINE_COUNT_INVALID",
                                "Invalid retry count for decline."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Count = retryCount;

                        if (!retryDeclineElement.TryGetProperty("delay", out var delayProp) || !delayProp.TryGetInt32(out var delay) || delay < 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_DECLINE_DELAY_INVALID",
                                "Invalid delay for decline retry."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Delay = delay;

                        if (!retryDeclineElement.TryGetProperty("unit", out var unitProp) || !unitProp.TryGetInt32(out int unitEnumInt) || !Enum.IsDefined(typeof(OutboundCallRetryDelayUnitType), unitEnumInt))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_DECLINE_UNIT_INVALID",
                                "Invalid unit for decline retry."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnDecline.Unit = (OutboundCallRetryDelayUnitType)unitEnumInt;
                    }

                    // Retry on Miss (similar logic to decline)
                    if (!configTabRootElement.TryGetProperty("retryOnMiss", out var retryMissElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_MISS_NOT_FOUND",
                            "Retry on miss settings not found."
                        );
                    }
                    if (!retryMissElement.TryGetProperty("enabled", out var retryMissEnabledProp) || (retryMissEnabledProp.ValueKind != JsonValueKind.True && retryMissEnabledProp.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_MISS_ENABLED_INVALID",
                            "Invalid 'enabled' value for retry on miss."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.RetryOnMiss.Enabled = retryMissEnabledProp.GetBoolean();
                    if (newBusinessAppCampaignData.Configuration.RetryOnMiss.Enabled)
                    {
                        if (!retryMissElement.TryGetProperty("count", out var retryCountProp) || !retryCountProp.TryGetInt32(out var retryCount) || retryCount < 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_MISS_COUNT_INVALID",
                                "Invalid retry count for miss."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Count = retryCount;

                        if (!retryMissElement.TryGetProperty("delay", out var delayProp) || !delayProp.TryGetInt32(out var delay) || delay < 1)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_MISS_DELAY_INVALID",
                                "Invalid delay for miss retry."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Delay = delay;

                        if (!retryMissElement.TryGetProperty("unit", out var unitProp) || !unitProp.TryGetInt32(out int unitEnumInt) || !Enum.IsDefined(typeof(OutboundCallRetryDelayUnitType), unitEnumInt))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:CONFIG_RETRY_MISS_UNIT_INVALID",
                                "Invalid unit for miss retry."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.RetryOnMiss.Unit = (OutboundCallRetryDelayUnitType)unitEnumInt;
                    }

                    // Timeouts
                    if (!configTabRootElement.TryGetProperty("timeouts", out var timeoutsElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_TIMEOUTS_NOT_FOUND",
                            "Timeouts settings not found."
                            );
                    }
                    if (!timeoutsElement.TryGetProperty("pickupDelayMS", out var pickupDelayProp) || !pickupDelayProp.TryGetInt32(out var pickupDelay) || pickupDelay < 0)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_PICKUP_DELAY_INVALID",
                            "Invalid pickup delay value."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.Timeouts.PickupDelayMS = pickupDelay;

                    if (!timeoutsElement.TryGetProperty("notifyOnSilenceMS", out var notifySilenceProp) || !notifySilenceProp.TryGetInt32(out var notifySilence) || notifySilence < 0)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_NOTIFY_SILENCE_INVALID",
                            "Invalid notify on silence value."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.Timeouts.NotifyOnSilenceMS = notifySilence;

                    if (!timeoutsElement.TryGetProperty("endOnSilenceMS", out var endSilenceProp) || !endSilenceProp.TryGetInt32(out var endSilence) || endSilence < 0)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_END_SILENCE_INVALID",
                            "Invalid end call on silence value."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.Timeouts.EndOnSilenceMS = endSilence;

                    if (!timeoutsElement.TryGetProperty("maxCallTimeS", out var maxCallTimeProp) || !maxCallTimeProp.TryGetInt32(out var maxCallTime) || maxCallTime < 0)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:CONFIG_MAX_CALL_TIME_INVALID",
                            "Invalid max call time value."
                        );
                    }
                    newBusinessAppCampaignData.Configuration.Timeouts.MaxCallTimeS = maxCallTime;
                }

                // Number Route Tab
                if (!changes.RootElement.TryGetProperty("numberRoute", out var numberRouteProperty) || numberRouteProperty.ValueKind != JsonValueKind.Object)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:NUMBER_ROUTE_TAB_NOT_FOUND",
                        "Number Route not found."
                    );
                }
                else
                {
                    if (!numberRouteProperty.TryGetProperty("defaultNumberId", out var defaultNumberIdProperty) || string.IsNullOrWhiteSpace(defaultNumberIdProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:DEFAULT_NUMBER_ID_NOT_FOUND",
                            "Default number id not found or invalid."
                        );
                    }
                    var defaultNumberIdValue = defaultNumberIdProperty.GetString()!;
                    if (!businessNumbers.Any(x => x.Id == defaultNumberIdValue))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:DEFAULT_NUMBER_ID_NOT_FOUND_IN_BUSINESS",
                            "Default number id not found in business numbers list."
                        );
                    }
                    newBusinessAppCampaignData.NumberRoute.DefaultNumberId = defaultNumberIdValue;

                    if (!numberRouteProperty.TryGetProperty("routeNumberList", out var routeNumberListProperty) || routeNumberListProperty.ValueKind != JsonValueKind.Object)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:ROUTE_NUMBER_LIST_NOT_FOUND",
                            "Route number list not found or invalid."
                        );
                    }
                    foreach (var routeNumberItem in routeNumberListProperty.EnumerateObject())
                    {
                        var routeNumberValue = routeNumberItem.Value.GetString();
                        if (string.IsNullOrEmpty(routeNumberItem.Name) || string.IsNullOrWhiteSpace(routeNumberValue))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:ROUTE_NUMBER_LIST_ITEM_INVALID",
                                $"Route number list item is invalid for country code route '{routeNumberItem.Name}'."
                            );
                        }
                        if (PhoneNumberUtil.GetInstance().GetCountryCodeForRegion(routeNumberItem.Name) == 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:ROUTE_NUMBER_LIST_ITEM_REGION_CODE_INVALID",
                                $"Route number list item region code is invalid for country code route '{routeNumberItem.Name}'."
                            );
                        }
                        if (!businessNumbers.Any(x => x.Id == routeNumberValue))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:ROUTE_NUMBER_LIST_ITEM_NOT_FOUND_IN_BUSINESS",
                                $"Route number list item not found for country code route {routeNumberItem.Name}."
                            );
                        }
                        newBusinessAppCampaignData.NumberRoute.RouteNumberList.Add(routeNumberItem.Name, routeNumberValue);
                    }
                }

                // Variables Tab
                if (!changes.RootElement.TryGetProperty("variables", out var variablesElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:VARIABLES_SECTION_MISSING",
                        "Variables section 'variables' not found."
                    );
                }
                else
                {
                    // Dynamic Variables
                    if (!variablesElement.TryGetProperty("dynamicVariables", out var dynamicVariablesElement) ||
                        dynamicVariablesElement.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLES_SECTION_MISSING",
                            "Variables section 'dynamicVariables' not found or not an array."
                        );
                    }
                    else
                    {
                        var dynamicVariablesEnumerator = dynamicVariablesElement.EnumerateArray().GetEnumerator();

                        foreach (var dynamicVariableElement in dynamicVariablesEnumerator)
                        {
                            var dynamicVariable = new BusinessAppCampaignVariableData();

                            if (!dynamicVariableElement.TryGetProperty("key", out var nameElement)
                                || nameElement.ValueKind != JsonValueKind.String)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLE_KEY_INVALID",
                                    "Invalid dynamic variable key or not found."
                                );
                            }
                            else
                            {
                                var key = nameElement.GetString();
                                if (string.IsNullOrWhiteSpace(key))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLE_KEY_EMPTY",
                                        "Dynamic variable key is empty."
                                    );
                                }

                                if (newBusinessAppCampaignData.Variables.DynamicVariables.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLE_KEY_EXISTS",
                                        $"Dynamic variable key '{key}' already exists."
                                    );
                                }

                                dynamicVariable.Key = key;
                            }

                            if (!dynamicVariableElement.TryGetProperty("isRequired", out var valueElement)
                                || (valueElement.ValueKind != JsonValueKind.True && valueElement.ValueKind != JsonValueKind.False)
                            )
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLE_ISREQUIRED_INVALID",
                                    "Invalid dynamic variable is required or not found."
                                );
                            }
                            else
                            {
                                dynamicVariable.IsRequired = valueElement.GetBoolean();
                            }

                            if (!dynamicVariableElement.TryGetProperty("isEmptyOrNullAllowed", out var isEmptyOrNullAllowedElement)
                                || (isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.True && isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.False))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:DYNAMIC_VARIABLE_ISREQUIRED_INVALID",
                                    "Invalid dynamic variable is required or not found."
                                );
                            }
                            else
                            {
                                dynamicVariable.IsEmptyOrNullAllowed = isEmptyOrNullAllowedElement.GetBoolean();
                            }

                            newBusinessAppCampaignData.Variables.DynamicVariables.Add(dynamicVariable);
                        }
                    }

                    // Metadata Variables
                    if (!variablesElement.TryGetProperty("metadata", out var metadataListElement) ||
                        metadataListElement.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:METADATA_SECTION_MISSING",
                            "Variables section 'metadata' not found or not an array."
                        );
                    }
                    else
                    {
                        var metadataListEnumerator = metadataListElement.EnumerateArray().GetEnumerator();

                        foreach (var metadataVariableElement in metadataListEnumerator)
                        {
                            var metadata = new BusinessAppCampaignVariableData();

                            if (!metadataVariableElement.TryGetProperty("key", out var nameElement)
                                || nameElement.ValueKind != JsonValueKind.String)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:METADATA_KEY_INVALID",
                                    "Invalid metadata key or not found."
                                );
                            }
                            else
                            {
                                var key = nameElement.GetString();
                                if (string.IsNullOrWhiteSpace(key))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:METADATA_KEY_EMPTY",
                                        "Metadata key is empty."
                                    );
                                }

                                if (newBusinessAppCampaignData.Variables.Metadata.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:METADATA_KEY_EXISTS",
                                        $"Metadata key '{key}' already exists."
                                    );
                                }

                                metadata.Key = key;
                            }

                            if (!metadataVariableElement.TryGetProperty("isRequired", out var valueElement)
                                || (valueElement.ValueKind != JsonValueKind.True && valueElement.ValueKind != JsonValueKind.False)
                            )
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:METADATA_ISREQUIRED_INVALID",
                                    "Invalid metadata is required or not found."
                                );
                            }
                            else
                            {
                                metadata.IsRequired = valueElement.GetBoolean();
                            }

                            if (!metadataVariableElement.TryGetProperty("isEmptyOrNullAllowed", out var isEmptyOrNullAllowedElement)
                                || (isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.True && isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.False))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:METADATA_ISREQUIRED_INVALID",
                                    "Invalid metadata is required or not found."
                                );
                            }
                            else
                            {
                                metadata.IsEmptyOrNullAllowed = isEmptyOrNullAllowedElement.GetBoolean();
                            }

                            newBusinessAppCampaignData.Variables.Metadata.Add(metadata);
                        }
                    }
                }

                // Voicemail Tab
                if (!changes.RootElement.TryGetProperty("voicemailDetection", out var voicemailElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_SECTION_MISSING",
                        "Voicemail section 'voicemailDetection' not found."
                    );
                }
                else
                {
                    var voicemailData = new BusinessAppTelephonyCampaignVoicemailDetection();

                    if (!voicemailElement.TryGetProperty("isEnabled", out var isEnabledElement)
                        || (isEnabledElement.ValueKind != JsonValueKind.True && isEnabledElement.ValueKind != JsonValueKind.False))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ISENABLED_INVALID",
                            "Voicemail isEnabled parameter is missing or invalid."
                        );
                    }
                    voicemailData.IsEnabled = isEnabledElement.GetBoolean();

                    if (voicemailData.IsEnabled)
                    {
                        if (!voicemailElement.TryGetProperty("initialCheckDelayMS", out var initialCheckDelayMSElement)
                            || initialCheckDelayMSElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_INITIALCHECKDELAY_INVALID",
                                "Voicemail initialCheckDelayMS parameter is missing or invalid."
                                );
                        }
                        voicemailData.InitialCheckDelayMS = initialCheckDelayMSElement.GetInt32();

                        if (!voicemailElement.TryGetProperty("voiceMailMessageVADSilenceThresholdMS", out var vadSilenceThresholdElement)
                            || vadSilenceThresholdElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_VADSILENCE_INVALID",
                                "Voicemail voiceMailMessageVADSilenceThresholdMS parameter is missing or invalid."
                                );
                        }
                        voicemailData.VoiceMailMessageVADSilenceThresholdMS = vadSilenceThresholdElement.GetInt32();

                        if (!voicemailElement.TryGetProperty("voiceMailMessageVADMaxSpeechDurationMS", out var vadMaxSpeechDurationElement)
                            || vadMaxSpeechDurationElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_VADMAXSPEECH_INVALID",
                                "Voicemail voiceMailMessageVADMaxSpeechDurationMS parameter is missing or invalid."
                                );
                        }
                        voicemailData.VoiceMailMessageVADMaxSpeechDurationMS = vadMaxSpeechDurationElement.GetInt32();

                        if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterMlCheckSuccess", out var stopOnMlElement)
                            || (stopOnMlElement.ValueKind != JsonValueKind.True && stopOnMlElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_STOPONML_INVALID",
                                "Voicemail stopSpeakingAgentAfterMlCheckSuccess parameter is missing or invalid."
                                );
                        }
                        voicemailData.StopSpeakingAgentAfterMlCheckSuccess = stopOnMlElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterVadSilence", out var stopOnVadElement)
                            || (stopOnVadElement.ValueKind != JsonValueKind.True && stopOnVadElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_STOPONVAD_INVALID",
                                "Voicemail stopSpeakingAgentAfterVadSilence parameter is missing or invalid."
                                );
                        }
                        voicemailData.StopSpeakingAgentAfterVadSilence = stopOnVadElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("stopSpeakingAgentAfterLLMConfirm", out var stopOnLlmElement)
                            || (stopOnLlmElement.ValueKind != JsonValueKind.True && stopOnLlmElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_STOPONLLM_INVALID",
                                "Voicemail stopSpeakingAgentAfterLLMConfirm parameter is missing or invalid."
                                );
                        }
                        voicemailData.StopSpeakingAgentAfterLLMConfirm = stopOnLlmElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("stopSpeakingAgentDelayAfterMatchMS", out var stopDelayElement)
                            || stopDelayElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_STOPDELAY_INVALID",
                                "Voicemail stopSpeakingAgentDelayAfterMatchMS parameter is missing or invalid."
                                );
                        }
                        voicemailData.StopSpeakingAgentDelayAfterMatchMS = stopDelayElement.GetInt32();

                        if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterMLCheckSuccess", out var endOnMlElement)
                            || (endOnMlElement.ValueKind != JsonValueKind.True && endOnMlElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDONML_INVALID",
                                "Voicemail endOrLeaveMessageAfterMLCheckSuccess parameter is missing or invalid."
                                );
                        }
                        voicemailData.EndOrLeaveMessageAfterMLCheckSuccess = endOnMlElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterVadSilence", out var endOnVadElement)
                            || (endOnVadElement.ValueKind != JsonValueKind.True && endOnVadElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDONVAD_INVALID",
                                "Voicemail endOrLeaveMessageAfterVadSilence parameter is missing or invalid."
                                );
                        }
                        voicemailData.EndOrLeaveMessageAfterVadSilence = endOnVadElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("endOrLeaveMessageAfterLLMConfirm", out var endOnLlmElement)
                            || (endOnLlmElement.ValueKind != JsonValueKind.True && endOnLlmElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDONLLM_INVALID",
                                "Voicemail endOrLeaveMessageAfterLLMConfirm parameter is missing or invalid."
                                );
                        }
                        voicemailData.EndOrLeaveMessageAfterLLMConfirm = endOnLlmElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("endOrLeaveMessageDelayAfterMatchMS", out var endLeaveDelayElement)
                            || endLeaveDelayElement.ValueKind != JsonValueKind.Number)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDDELAY_INVALID",
                                "Voicemail endOrLeaveMessageDelayAfterMatchMS parameter is missing or invalid."
                                );
                        }
                        voicemailData.EndOrLeaveMessageDelayAfterMatchMS = endLeaveDelayElement.GetInt32();

                        if (!voicemailElement.TryGetProperty("endCallOnDetect", out var endCallElement)
                            || (endCallElement.ValueKind != JsonValueKind.True && endCallElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDCALL_INVALID",
                                "Voicemail endCallOnDetect parameter is missing or invalid."
                                );
                        }
                        voicemailData.EndCallOnDetect = endCallElement.GetBoolean();

                        if (!voicemailElement.TryGetProperty("leaveMessageOnDetect", out var leaveMessageElement)
                            || (leaveMessageElement.ValueKind != JsonValueKind.True && leaveMessageElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_LEAVEMESSAGE_INVALID",
                                "Voicemail leaveMessageOnDetect parameter is missing or invalid."
                                );
                        }
                        voicemailData.LeaveMessageOnDetect = leaveMessageElement.GetBoolean();

                        if (voicemailData.EndCallOnDetect && voicemailData.LeaveMessageOnDetect)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ENDCALLANDLEAVEMESSAGE_INVALID",
                                "Voicemail endCallOnDetect and leaveMessageOnDetect cannot be true at the same time."
                                );
                        }

                        if (voicemailData.LeaveMessageOnDetect)
                        {
                            voicemailData.MessageToLeave = new Dictionary<string, string>();
                            var messageToLeaveValidationResult = MultiLanguagePropertyHelper.ValidateAndAssignMultiLanguageProperty(businessLanguages, voicemailElement, "messageToLeave", voicemailData.MessageToLeave);
                            if (!messageToLeaveValidationResult.Success)
                            {
                                return result.SetFailureResult("AddOrUpdateTelephonyCampaignAsync:" + messageToLeaveValidationResult.Code, messageToLeaveValidationResult.Message);
                            }
                        }

                        if (!voicemailElement.TryGetProperty("onVoiceMailMessageDetectVerifySTTAndLLM", out var advancedVerificationElement)
                            || (advancedVerificationElement.ValueKind != JsonValueKind.True && advancedVerificationElement.ValueKind != JsonValueKind.False))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_ADVANCEDVERIFICATION_INVALID",
                                "Voicemail onVoiceMailMessageDetectVerifySTTAndLLM parameter is missing or invalid."
                                );
                        }
                        voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM = advancedVerificationElement.GetBoolean();

                        if ((voicemailData.StopSpeakingAgentAfterLLMConfirm || voicemailData.EndOrLeaveMessageAfterLLMConfirm)
                            && !voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_LLMTRIGGER_MISMATCH",
                                "An LLM Confirmation trigger is enabled, but Advanced Verification is disabled."
                                );
                        }

                        if (voicemailData.OnVoiceMailMessageDetectVerifySTTAndLLM)
                        {
                            if (!voicemailElement.TryGetProperty("transcribeVoiceMessageSTT", out var sttIntegrationElement)
                                || sttIntegrationElement.ValueKind == JsonValueKind.Null)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_STT_INTEGRATION_MISSING",
                                    "STT integration for voicemail advanced verification is required but not provided."
                                    );
                            }
                            var sttValidationResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(businessId, sttIntegrationElement, "STT");
                            if (!sttValidationResult.Success || sttValidationResult.Data == null)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:" + sttValidationResult.Code,
                                    "Voicemail STT Integration failed: " + sttValidationResult.Message
                                    );
                            }
                            voicemailData.TranscribeVoiceMessageSTT = sttValidationResult.Data;

                            if (!voicemailElement.TryGetProperty("verifyVoiceMessageLLM", out var llmIntegrationElement)
                                || llmIntegrationElement.ValueKind == JsonValueKind.Null)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:VOICEMAIL_LLM_INTEGRATION_MISSING",
                                    "LLM integration for voicemail advanced verification is required but not provided."
                                    );
                            }
                            var llmValidationResult = await _integrationConfigurationManager.ValidateAndBuildIntegrationData(businessId, llmIntegrationElement, "LLM");
                            if (!llmValidationResult.Success || llmValidationResult.Data == null)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateTelephonyCampaignAsync:" + llmValidationResult.Code,
                                    "Voicemail LLM Integration failed: " + llmValidationResult.Message
                                    );
                            }
                            voicemailData.VerifyVoiceMessageLLM = llmValidationResult.Data;
                        }
                    }

                    newBusinessAppCampaignData.VoicemailDetection = voicemailData;
                }

                // Post Analysis Tab
                if (!changes.RootElement.TryGetProperty("postAnalysis", out var postAnalysisElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_TAB_NOT_FOUND",
                        "Post analysis tab not found."
                    );
                }
                else
                {
                    if (!postAnalysisElement.TryGetProperty("postAnalysisId", out var postAnalysisIdValue)
                        || (postAnalysisIdValue.ValueKind != JsonValueKind.String && postAnalysisIdValue.ValueKind != JsonValueKind.Null)
                        || (postAnalysisIdValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(postAnalysisIdValue.GetString()))
                    )
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_TEMPLATE_ID_NOT_FOUND",
                            "Post analysis 'postAnalysisId' not found or invalid."
                        );
                    }

                    if (postAnalysisIdValue.ValueKind == JsonValueKind.String)
                    {
                        newBusinessAppCampaignData.PostAnalysis.PostAnalysisId = postAnalysisIdValue.GetString()!;

                        if (!postAnalysisElement.TryGetProperty("contextVariables", out var contextVariablesElement) ||
                            contextVariablesElement.ValueKind != JsonValueKind.Array)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLES_NOT_FOUND",
                                "Post analysis 'contextVariables' not found or not an array."
                            );
                        }
                        else
                        {
                            newBusinessAppCampaignData.PostAnalysis.ContextVariables = new List<BusinessAppCampaignPostAnalysisContextVariable>();

                            foreach (var contextVariableElement in contextVariablesElement.EnumerateArray())
                            {
                                var contextVariable = new BusinessAppCampaignPostAnalysisContextVariable();

                                if (!contextVariableElement.TryGetProperty("name", out var nameElement) ||
                                    nameElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(nameElement.GetString())
                                )
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_NAME_NOT_FOUND",
                                        "Post analysis context variable 'name' not found or invalid."
                                    );
                                }
                                contextVariable.Name = nameElement.GetString()!;

                                if (!contextVariableElement.TryGetProperty("description", out var descriptionElement) ||
                                    descriptionElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(descriptionElement.GetString())
                                )
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_DESCRIPTION_NOT_FOUND",
                                        "Post analysis context variable 'description' not found or invalid."
                                    );
                                }
                                contextVariable.Description = descriptionElement.GetString()!;

                                if (!contextVariableElement.TryGetProperty("value", out var valueElement) ||
                                    valueElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(valueElement.GetString())
                                )
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_VALUE_NOT_FOUND",
                                        "Post analysis context variable 'value' not found or invalid."
                                    );
                                }
                                contextVariable.Value = valueElement.GetString()!;

                                var valueTemplateValidation = CustomVariableInputTemplateValidator.Validate(contextVariable.Value, TelephonyCampaginPostAnalysisContextVariableArguementsList);
                                if (!valueTemplateValidation.IsValid)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateTelephonyCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_VALUE_INVALID",
                                        $"Post analysis context variable 'value' is invalid:\n\n{string.Join("\n", valueTemplateValidation.Errors)}"
                                    );
                                }

                                newBusinessAppCampaignData.PostAnalysis.ContextVariables.Add(contextVariable);
                            }
                        }
                    }
                }


                // Telephony Actions Tab
                if (!changes.RootElement.TryGetProperty("actions", out var telephonyActionsTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateTelephonyCampaignAsync:TELEPHONY_ACTIONS_TAB_NOT_FOUND",
                        "Telephony actions tab not found."
                    );
                }
                else
                {
                    var callInitiationFailureToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callInitiationFailureTool"), "CallInitiationFailure");
                    if (!callInitiationFailureToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callInitiationFailureToolValidationResult.Code,
                            callInitiationFailureToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallInitiationFailureTool = callInitiationFailureToolValidationResult.Data;

                    var callInitiatedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callInitiatedTool"), "CallInitiated");
                    if (!callInitiatedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callInitiatedToolValidationResult.Code,
                            callInitiatedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallInitiatedTool = callInitiatedToolValidationResult.Data;

                    var callDeclinedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callDeclinedTool"), "CallDeclined");
                    if (!callDeclinedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callDeclinedToolValidationResult.Code,
                            callDeclinedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallDeclinedTool = callDeclinedToolValidationResult.Data;

                    var callMissedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callMissedTool"), "CallMissed");
                    if (!callMissedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callMissedToolValidationResult.Code,
                            callMissedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallMissedTool = callMissedToolValidationResult.Data;

                    var callAnsweredToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callAnsweredTool"), "CallAnswered");
                    if (!callAnsweredToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callAnsweredToolValidationResult.Code,
                            callAnsweredToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallAnsweredTool = callAnsweredToolValidationResult.Data;

                    var callEndedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], telephonyActionsTabRootElement.GetProperty("callEndedTool"), "CallEnded");
                    if (!callEndedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:" + callEndedToolValidationResult.Code,
                            callEndedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.CallEndedTool = callEndedToolValidationResult.Data;
                }

                // Save or Update in Database
                if (postType == "new")
                {
                    newBusinessAppCampaignData.Id = Guid.NewGuid().ToString();
                    var addResult = await _businessAppRepository.AddBusinessAppTelephonyCampaign(businessId, newBusinessAppCampaignData);
                    if (!addResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:DB_ADD_FAILED",
                            "Failed to add business app telephony campaign."
                        );
                    }
                }
                else // postType == "edit"
                {
                    newBusinessAppCampaignData.Id = existingCampaignData.Id;
                    var updateResult = await _businessAppRepository.UpdateBusinessAppTelephonyCampaign(businessId, newBusinessAppCampaignData);
                    if (!updateResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateTelephonyCampaignAsync:DB_UPDATE_FAILED",
                            "Failed to update business app telephony campaign."
                        );
                    }
                }

                return result.SetSuccessResult(newBusinessAppCampaignData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddOrUpdateTelephonyCampaignAsync:EXCEPTION",
                    $"Error adding or updating telephony campaign: {ex.Message}"
                );
            }
        }

        /**
         * 
         * Web Campaign
         * 
        **/
        public async Task<FunctionReturnResult<BusinessAppWebCampaign?>> GetWebCampaignById(long businessId, string existingWebCampaignId)
        {
            var result = new FunctionReturnResult<BusinessAppWebCampaign?>();

            try
            {
                var data = await _businessAppRepository.GetBusinessWebCampaignById(businessId, existingWebCampaignId);
                if (data == null)
                {
                    return result.SetFailureResult(
                        "GetWebCampaignById:NOT_FOUND",
                        "Campaign not found."
                    );
                }

                return result.SetSuccessResult(data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebCampaignById:EXCEPTION",
                    $"Error retrieving campaign: {ex.Message}"
                );
            }
        }
        public async Task<FunctionReturnResult<BusinessAppWebCampaign?>> AddOrUpdateWebCampaignAsync(long businessId, IFormCollection formData, string postType, BusinessAppWebCampaign? existingCampaignData)
        {
            var result = new FunctionReturnResult<BusinessAppWebCampaign?>();

            try
            {
                var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);

                if (!formData.TryGetValue("changes", out var changesJsonString) || string.IsNullOrWhiteSpace(changesJsonString))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:CHANGES_NOT_FOUND",
                        "Changes not found or is empty in form data."
                    );
                }

                JsonDocument changes;
                try
                {
                    changes = JsonDocument.Parse(changesJsonString!);
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:CHANGES_PARSE_FAILED",
                        $"Unable to parse changes json string: {ex.Message}"
                    );
                }

                var newBusinessAppCampaignData = new BusinessAppWebCampaign();

                // General Tab
                if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:GENERAL_TAB_NOT_FOUND",
                        "General tab not found."
                    );
                }
                else
                {
                    if (!generalTabRootElement.TryGetProperty("emoji", out var generalEmojiProperty) || string.IsNullOrWhiteSpace(generalEmojiProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:GENERAL_EMOJI_IS_REQUIRED",
                            "General emoji is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Emoji = generalEmojiProperty.GetString()!;

                    if (!generalTabRootElement.TryGetProperty("name", out var generalNameProperty) || string.IsNullOrWhiteSpace(generalNameProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:GENERAL_NAME_IS_REQUIRED",
                            "General name is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Name = generalNameProperty.GetString()!;

                    if (!generalTabRootElement.TryGetProperty("description", out var generalDescriptionProperty) || string.IsNullOrWhiteSpace(generalDescriptionProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:GENERAL_DESCRIPTION_IS_REQUIRED",
                            "General description is required."
                        );
                    }
                    newBusinessAppCampaignData.General.Description = generalDescriptionProperty.GetString()!;
                }

                // Agent Tab
                if (!changes.RootElement.TryGetProperty("agent", out var agentTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:AGENT_TAB_NOT_FOUND",
                        "Agent tab not found."
                    );
                }
                else
                {
                    if (!agentTabRootElement.TryGetProperty("selectedAgentId", out var selectedAgentIdProperty) || string.IsNullOrWhiteSpace(selectedAgentIdProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_ID_IS_REQUIRED",
                            "Selected agent id is required."
                        );
                    }
                    var selectedAgentId = selectedAgentIdProperty.GetString()!;
                    var getBusinessAgent = await _businessAppRepository.GetAgentById(businessId, selectedAgentId);
                    if (getBusinessAgent == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_NOT_FOUND_IN_DB",
                            "Selected agent not found."
                        );
                    }
                    newBusinessAppCampaignData.Agent.SelectedAgentId = selectedAgentId;

                    if (!agentTabRootElement.TryGetProperty("openingScriptId", out var openingScriptIdProperty) || string.IsNullOrWhiteSpace(openingScriptIdProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_SCRIPT_ID_IS_REQUIRED",
                            "Opening script id is required."
                        );
                    }
                    var openingScriptId = openingScriptIdProperty.GetString()!;
                    if (getBusinessAgent.Scripts.Find(x => x.Id == openingScriptId) == null)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_SCRIPT_NOT_FOUND_IN_AGENT",
                            "Opening script not found within selected agent."
                        );
                    }
                    newBusinessAppCampaignData.Agent.OpeningScriptId = openingScriptId;

                    if (!agentTabRootElement.TryGetProperty("language", out var languageProperty) || string.IsNullOrWhiteSpace(languageProperty.GetString()))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_LANGUAGE_IS_REQUIRED",
                            "Language is required."
                        );
                    }
                    var language = languageProperty.GetString()!;
                    if (!businessLanguages.Contains(language))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_LANGUAGE_NOT_ENABLED",
                            $"Language {language} is not enabled for this business."
                        );
                    }
                    newBusinessAppCampaignData.Agent.Language = language;

                    if (!agentTabRootElement.TryGetProperty("timezones", out var timezonesProperty) || timezonesProperty.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:AGENT_TIMEZONES_NOT_FOUND",
                            "Timezones not found or invalid."
                        );
                    }
                    foreach (var timezone in timezonesProperty.EnumerateArray())
                    {
                        string? timezoneValue = timezone.GetString();
                        if (string.IsNullOrWhiteSpace(timezoneValue) || !TimeZoneHelper.ValidateOffsetString(timezoneValue))
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateWebCampaignAsync:AGENT_TIMEZONE_VALIDATION_FAILED",
                                $"Unable to validate timezone {timezoneValue}."
                            );
                        }
                        newBusinessAppCampaignData.Agent.Timezones.Add(timezoneValue);
                    }
                }

                // Configuration Tab
                if (!changes.RootElement.TryGetProperty("configuration", out var configTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:CONFIG_TAB_NOT_FOUND",
                        "Configuration tab not found."
                    );
                }
                else
                {
                    if (!configTabRootElement.TryGetProperty("timeouts", out var timeoutsElement))
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:CONFIG_TIMEOUTS_NOT_FOUND",
                            "Timeouts settings not found."
                        );
                    }
                    else
                    {
                        if (!timeoutsElement.TryGetProperty("notifyOnSilenceMS", out var notifySilenceProp) || !notifySilenceProp.TryGetInt32(out var notifySilence) || notifySilence < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateWebCampaignAsync:CONFIG_NOTIFY_SILENCE_INVALID",
                                "Invalid notify on silence value."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.Timeouts.NotifyOnSilenceMS = notifySilence;

                        if (!timeoutsElement.TryGetProperty("endOnSilenceMS", out var endSilenceProp) || !endSilenceProp.TryGetInt32(out var endSilence) || endSilence < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateWebCampaignAsync:CONFIG_END_SILENCE_INVALID",
                                "Invalid end conversation on silence value."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.Timeouts.EndOnSilenceMS = endSilence;

                        if (!timeoutsElement.TryGetProperty("maxConversationTimeS", out var maxConvoTimeProp) || !maxConvoTimeProp.TryGetInt32(out var maxConvoTime) || maxConvoTime < 0)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateWebCampaignAsync:CONFIG_MAX_CONVO_TIME_INVALID",
                                "Invalid max conversation time value."
                            );
                        }
                        newBusinessAppCampaignData.Configuration.Timeouts.MaxConversationTimeS = maxConvoTime;
                    }
                }

                // Post Analysis Tab
                if (!changes.RootElement.TryGetProperty("postAnalysis", out var postAnalysisElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_TAB_NOT_FOUND",
                        "Post analysis tab not found."
                    );
                }
                else
                {
                    if (!postAnalysisElement.TryGetProperty("postAnalysisId", out var postAnalysisIdValue)
                        || (postAnalysisIdValue.ValueKind != JsonValueKind.String && postAnalysisIdValue.ValueKind != JsonValueKind.Null)
                        || (postAnalysisIdValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(postAnalysisIdValue.GetString()))
                    ) {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_TEMPLATE_ID_NOT_FOUND",
                            "Post analysis 'postAnalysisId' not found or invalid."
                        );
                    }

                    if (postAnalysisIdValue.ValueKind == JsonValueKind.String)
                    {
                        newBusinessAppCampaignData.PostAnalysis.PostAnalysisId = postAnalysisIdValue.GetString()!;

                        if (!postAnalysisElement.TryGetProperty("contextVariables", out var contextVariablesElement) ||
                            contextVariablesElement.ValueKind != JsonValueKind.Array)
                        {
                            return result.SetFailureResult(
                                "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLES_NOT_FOUND",
                                "Post analysis 'contextVariables' not found or not an array."
                            );
                        }
                        else
                        {
                            newBusinessAppCampaignData.PostAnalysis.ContextVariables = new List<BusinessAppCampaignPostAnalysisContextVariable>();

                            foreach (var contextVariableElement in contextVariablesElement.EnumerateArray())
                            {
                                var contextVariable = new BusinessAppCampaignPostAnalysisContextVariable();

                                if (!contextVariableElement.TryGetProperty("name", out var nameElement) ||
                                    nameElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(nameElement.GetString())
                                ) {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_NAME_NOT_FOUND",
                                        "Post analysis context variable 'name' not found or invalid."
                                    );
                                }
                                contextVariable.Name = nameElement.GetString()!;

                                if (!contextVariableElement.TryGetProperty("description", out var descriptionElement) ||
                                    descriptionElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(descriptionElement.GetString())
                                ) {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_DESCRIPTION_NOT_FOUND",
                                        "Post analysis context variable 'description' not found or invalid."
                                    );
                                }
                                contextVariable.Description = descriptionElement.GetString()!;

                                if (!contextVariableElement.TryGetProperty("value", out var valueElement) ||
                                    valueElement.ValueKind != JsonValueKind.String ||
                                    string.IsNullOrWhiteSpace(valueElement.GetString())
                                ) {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_VALUE_NOT_FOUND",
                                        "Post analysis context variable 'value' not found or invalid."
                                    );
                                }
                                contextVariable.Value = valueElement.GetString()!;

                                var valueTemplateValidation = CustomVariableInputTemplateValidator.Validate(contextVariable.Value, WebCampaginPostAnalysisContextVariableArguementsList);
                                if (!valueTemplateValidation.IsValid)
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:POST_ANALYSIS_CONTEXT_VARIABLE_VALUE_INVALID",
                                        $"Post analysis context variable 'value' is invalid:\n\n{string.Join("\n", valueTemplateValidation.Errors)}"
                                    );
                                }

                                newBusinessAppCampaignData.PostAnalysis.ContextVariables.Add(contextVariable);
                            }
                        }
                    }
                }

                // Variables Tab
                if (!changes.RootElement.TryGetProperty("variables", out var variablesElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:VARIABLES_SECTION_MISSING",
                        "Variables section 'variables' not found."
                    );
                }
                else
                {
                    // Dynamic Variables
                    if (!variablesElement.TryGetProperty("dynamicVariables", out var dynamicVariablesElement) ||
                        dynamicVariablesElement.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLES_SECTION_MISSING",
                            "Variables section 'dynamicVariables' not found or not an array."
                        );
                    }
                    else
                    {
                        var dynamicVariablesEnumerator = dynamicVariablesElement.EnumerateArray().GetEnumerator();

                        foreach (var dynamicVariableElement in dynamicVariablesEnumerator)
                        {
                            var dynamicVariable = new BusinessAppCampaignVariableData();

                            if (!dynamicVariableElement.TryGetProperty("key", out var nameElement)
                                || nameElement.ValueKind != JsonValueKind.String)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLE_KEY_INVALID",
                                    "Invalid dynamic variable key or not found."
                                );
                            }
                            else
                            {
                                var key = nameElement.GetString();
                                if (string.IsNullOrWhiteSpace(key))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLE_KEY_EMPTY",
                                        "Dynamic variable key is empty."
                                    );
                                }

                                if (newBusinessAppCampaignData.Variables.DynamicVariables.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLE_KEY_EXISTS",
                                        $"Dynamic variable key '{key}' already exists."
                                    );
                                }

                                dynamicVariable.Key = key;
                            }

                            if (!dynamicVariableElement.TryGetProperty("isRequired", out var valueElement)
                                || (valueElement.ValueKind != JsonValueKind.True && valueElement.ValueKind != JsonValueKind.False)
                            )
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLE_ISREQUIRED_INVALID",
                                    "Invalid dynamic variable is required or not found."
                                );
                            }
                            else
                            {
                                dynamicVariable.IsRequired = valueElement.GetBoolean();
                            }

                            if (!dynamicVariableElement.TryGetProperty("isEmptyOrNullAllowed", out var isEmptyOrNullAllowedElement)
                                || (isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.True && isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.False))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:DYNAMIC_VARIABLE_ISREQUIRED_INVALID",
                                    "Invalid dynamic variable is required or not found."
                                );
                            }
                            else
                            {
                                dynamicVariable.IsEmptyOrNullAllowed = isEmptyOrNullAllowedElement.GetBoolean();
                            }

                            newBusinessAppCampaignData.Variables.DynamicVariables.Add(dynamicVariable);
                        }
                    }

                    // Metadata Variables
                    if (!variablesElement.TryGetProperty("metadata", out var metadataListElement) ||
                        metadataListElement.ValueKind != JsonValueKind.Array)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:METADATA_SECTION_MISSING",
                            "Variables section 'metadata' not found or not an array."
                        );
                    }
                    else
                    {
                        var metadataListEnumerator = metadataListElement.EnumerateArray().GetEnumerator();

                        foreach (var metadataVariableElement in metadataListEnumerator)
                        {
                            var metadata = new BusinessAppCampaignVariableData();

                            if (!metadataVariableElement.TryGetProperty("key", out var nameElement)
                                || nameElement.ValueKind != JsonValueKind.String)
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:METADATA_KEY_INVALID",
                                    "Invalid metadata key or not found."
                                );
                            }
                            else
                            {
                                var key = nameElement.GetString();
                                if (string.IsNullOrWhiteSpace(key))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:METADATA_KEY_EMPTY",
                                        "Metadata key is empty."
                                    );
                                }

                                if (newBusinessAppCampaignData.Variables.Metadata.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    return result.SetFailureResult(
                                        "AddOrUpdateWebCampaignAsync:METADATA_KEY_EXISTS",
                                        $"Metadata key '{key}' already exists."
                                    );
                                }

                                metadata.Key = key;
                            }

                            if (!metadataVariableElement.TryGetProperty("isRequired", out var valueElement)
                                || (valueElement.ValueKind != JsonValueKind.True && valueElement.ValueKind != JsonValueKind.False)
                            )
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:METADATA_ISREQUIRED_INVALID",
                                    "Invalid metadata is required or not found."
                                );
                            }
                            else
                            {
                                metadata.IsRequired = valueElement.GetBoolean();
                            }

                            if (!metadataVariableElement.TryGetProperty("isEmptyOrNullAllowed", out var isEmptyOrNullAllowedElement)
                                || (isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.True && isEmptyOrNullAllowedElement.ValueKind != JsonValueKind.False))
                            {
                                return result.SetFailureResult(
                                    "AddOrUpdateWebCampaignAsync:METADATA_ISREQUIRED_INVALID",
                                    "Invalid metadata is required or not found."
                                );
                            }
                            else
                            {
                                metadata.IsEmptyOrNullAllowed = isEmptyOrNullAllowedElement.GetBoolean();
                            }

                            newBusinessAppCampaignData.Variables.Metadata.Add(metadata);
                        }
                    }
                }

                // Web Actions Tab
                if (!changes.RootElement.TryGetProperty("actions", out var webActionsTabRootElement))
                {
                    return result.SetFailureResult(
                        "AddOrUpdateWebCampaignAsync:WEB_ACTIONS_TAB_NOT_FOUND",
                        "Web Actions tab not found."
                    );
                }
                else
                {
                    var conversationInitiationFailureToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], webActionsTabRootElement.GetProperty("conversationInitiationFailureTool"), "ConversationInitiationFailure");
                    if (!conversationInitiationFailureToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:" + conversationInitiationFailureToolValidationResult.Code,
                            conversationInitiationFailureToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.ConversationInitiationFailureTool = conversationInitiationFailureToolValidationResult.Data;

                    var conversationInitiatedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], webActionsTabRootElement.GetProperty("conversationInitiatedTool"), "ConversationInitiated");
                    if (!conversationInitiatedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:" + conversationInitiatedToolValidationResult.Code,
                            conversationInitiatedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.ConversationInitiatedTool = conversationInitiatedToolValidationResult.Data;

                    var conversationEndedToolValidationResult = await ValidateBusinessCampaignActionData(businessId, businessLanguages[0], webActionsTabRootElement.GetProperty("conversationEndedTool"), "ConversationEnded");
                    if (!conversationEndedToolValidationResult.Success)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:" + conversationEndedToolValidationResult.Code,
                            conversationEndedToolValidationResult.Message
                        );
                    }
                    newBusinessAppCampaignData.Actions.ConversationEndedTool = conversationEndedToolValidationResult.Data;
                }

                // Save or Update in Database
                if (postType == "new")
                {
                    newBusinessAppCampaignData.Id = Guid.NewGuid().ToString();
                    var addResult = await _businessAppRepository.AddBusinessAppWebCampaign(businessId, newBusinessAppCampaignData);
                    if (!addResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:DB_ADD_FAILED",
                            "Failed to add business app web campaign."
                        );
                    }
                }
                else // postType == "edit"
                {
                    newBusinessAppCampaignData.Id = existingCampaignData.Id;
                    var updateResult = await _businessAppRepository.UpdateBusinessAppWebCampaign(businessId, newBusinessAppCampaignData);
                    if (!updateResult)
                    {
                        return result.SetFailureResult(
                            "AddOrUpdateWebCampaignAsync:DB_UPDATE_FAILED",
                            "Failed to update business app web campaign."
                        );
                    }
                }

                return result.SetSuccessResult(newBusinessAppCampaignData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "AddOrUpdateWebCampaignAsync:EXCEPTION",
                    $"Error adding or updating web campaign: {ex.Message}"
                );
            }
        }

        // Common Helpers
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

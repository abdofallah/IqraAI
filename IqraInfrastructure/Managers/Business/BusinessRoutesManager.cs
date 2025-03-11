using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessRoutesManager
    {
        private readonly BusinessManager _parentBusinessManager;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;

        public BusinessRoutesManager(BusinessManager businessManager, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository)
        {
            _parentBusinessManager = businessManager;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
        }

        public async Task<bool> CheckBusinessRouteExists(long businessId, string existingRouteId)
        {
            return await _businessAppRepository.CheckBusinessRouteExists(businessId, existingRouteId);
        }

        public async Task<BusinessAppRoute?> GetBusinessRoute(long businessId, string existingRouteId)
        {
            return await _businessAppRepository.GetBusinessRoute(businessId, existingRouteId);
        }

        public async Task<FunctionReturnResult<BusinessAppRoute?>> AddOrUpdateUserBusinessRoute(long businessId,  IFormCollection formData, string postType, BusinessAppRoute? existingRouteData)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            var businessLanguages = await _businessRepository.GetBusinessLanguages(businessId);
            var businessNumbers = await _businessAppRepository.GetBusinessNumbers(businessId);

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:1";
                result.Message = "Changes not found in form data.";
                return result;
            }

            JsonDocument? changes = JsonDocument.Parse(changesJsonString);
            if (changes == null)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:2";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            var newBusinessAppRouteData = new BusinessAppRoute();

            // General Tab
            if (!changes.RootElement.TryGetProperty("general", out var generalTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:3";
                result.Message = "General tab not found.";
                return result;
            }

            if (!generalTabRootElement.TryGetProperty("emoji", out var generalEmojiProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:4";
                result.Message = "General emoji not found.";
                return result;
            }
            string? emoji = generalEmojiProperty.GetString();
            if (string.IsNullOrWhiteSpace(emoji))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:5";
                result.Message = "General emoji is required.";
                return result;
            }
            newBusinessAppRouteData.General.Emoji = emoji;

            if (!generalTabRootElement.TryGetProperty("name", out var generalNameProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:6";
                result.Message = "General name not found.";
                return result;
            }
            string? name = generalNameProperty.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:7";
                result.Message = "General name is required.";
                return result;
            }
            newBusinessAppRouteData.General.Name = name;

            if (!generalTabRootElement.TryGetProperty("description", out var generalDescriptionProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:8";
                result.Message = "General description not found.";
                return result;
            }
            string? description = generalDescriptionProperty.GetString();
            if (string.IsNullOrWhiteSpace(description))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:9";
                result.Message = "General description is required.";
                return result;
            }
            newBusinessAppRouteData.General.Description = description;

            // Language Tab
            if (!changes.RootElement.TryGetProperty("language", out var languageTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:10";
                result.Message = "Language tab not found.";
                return result;
            }

            if (!languageTabRootElement.TryGetProperty("defaultLanguageCode", out var defaultLanguageCodeProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:11";
                result.Message = "Default language code not found.";
                return result;
            }
            string? defaultLanguageCode = defaultLanguageCodeProperty.GetString();
            if (string.IsNullOrWhiteSpace(defaultLanguageCode))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:12";
                result.Message = "Default language code is required.";
                return result;
            }
            if (!businessLanguages.Contains(defaultLanguageCode))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:13";
                result.Message = "Default language code is not added for business.";
                return result;
            }
            newBusinessAppRouteData.Language.DefaultLanguageCode = defaultLanguageCode;       

            if (!languageTabRootElement.TryGetProperty("multiLanguageEnabled", out var multiLanguageEnabledProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:13";
                result.Message = "Multi language enabled property not found.";
                return result;
            }
            newBusinessAppRouteData.Language.MultiLanguageEnabled = multiLanguageEnabledProperty.GetBoolean();

            if (newBusinessAppRouteData.Language.MultiLanguageEnabled)
            {
                if (!languageTabRootElement.TryGetProperty("enabledMultiLanguages", out var enabledMultiLanguagesProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:14";
                    result.Message = "Enabled multi languages not found.";
                    return result;
                }

                newBusinessAppRouteData.Language.EnabledMultiLanguages = new List<BusinessAppRouteLanguageMultiEnabled>();
                foreach (var enabledLanguage in enabledMultiLanguagesProperty.EnumerateArray())
                {
                    if (!enabledLanguage.TryGetProperty("languageCode", out var languageCodeProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:15";
                        result.Message = "Language code not found in enabled language.";
                        return result;
                    }
                    string? languageCode = languageCodeProperty.GetString();
                    if (string.IsNullOrWhiteSpace(languageCode))
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:16";
                        result.Message = "Language code is required in enabled language.";
                        return result;
                    }

                    if (!businessLanguages.Contains(languageCode))
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:17";
                        result.Message = "Multilanguage " + languageCode + " is not added for business.";
                        return result;
                    }

                    if (!enabledLanguage.TryGetProperty("messageToPlay", out var messageToPlayProperty))
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:17";
                        result.Message = "Message to play not found in enabled language.";
                        return result;
                    }
                    string? messageToPlay = messageToPlayProperty.GetString();
                    if (string.IsNullOrWhiteSpace(messageToPlay))
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:18";
                        result.Message = "Message to play is required in enabled language.";
                        return result;
                    }

                    newBusinessAppRouteData.Language.EnabledMultiLanguages.Add(new BusinessAppRouteLanguageMultiEnabled
                    {
                        LanguageCode = languageCode,
                        MessageToPlay = messageToPlay
                    });
                }
            }

            // Configuration Tab
            if (!changes.RootElement.TryGetProperty("configuration", out var configurationTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:19";
                result.Message = "Configuration tab not found.";
                return result;
            }

            if (!configurationTabRootElement.TryGetProperty("pickUpDelayMS", out var pickUpDelayMSProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:20";
                result.Message = "Pick up delay not found.";
                return result;
            }
            if (!pickUpDelayMSProperty.TryGetInt32(out var pickUpDelayMS) || pickUpDelayMS < 0)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:21";
                result.Message = "Invalid pick up delay value.";
                return result;
            }
            newBusinessAppRouteData.Configuration.PickUpDelayMS = pickUpDelayMS;

            if (!configurationTabRootElement.TryGetProperty("notifyOnSilenceMS", out var notifyOnSilenceMSProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:22";
                result.Message = "Notify on silence not found.";
                return result;
            }
            if (!notifyOnSilenceMSProperty.TryGetInt32(out var notifyOnSilenceMS) || notifyOnSilenceMS < 0)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:23";
                result.Message = "Invalid notify on silence value.";
                return result;
            }
            newBusinessAppRouteData.Configuration.NotifyOnSilenceMS = notifyOnSilenceMS;

            if (!configurationTabRootElement.TryGetProperty("endCallOnSilenceMS", out var endCallOnSilenceMSProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:24";
                result.Message = "End call on silence not found.";
                return result;
            }
            if (!endCallOnSilenceMSProperty.TryGetInt32(out var endCallOnSilenceMS) || endCallOnSilenceMS < 0)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:25";
                result.Message = "Invalid end call on silence value.";
                return result;
            }
            newBusinessAppRouteData.Configuration.EndCallOnSilenceMS = endCallOnSilenceMS;

            if (!configurationTabRootElement.TryGetProperty("maxCallTimeS", out var maxCallTimeSProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:26";
                result.Message = "Max call time not found.";
                return result;
            }
            if (!maxCallTimeSProperty.TryGetInt32(out var maxCallTimeS) || maxCallTimeS < 0 || maxCallTimeS > 1800)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:27";
                result.Message = "Invalid max call time value (min 0, max 1800).";
                return result;
            }
            newBusinessAppRouteData.Configuration.MaxCallTimeS = maxCallTimeS;

            // Numbers Tab
            if (!changes.RootElement.TryGetProperty("numbers", out var numbersProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:28";
                result.Message = "Numbers not found.";
                return result;
            }

            newBusinessAppRouteData.Numbers = new List<string>();
            foreach (var number in numbersProperty.EnumerateArray())
            {
                string? numberId = number.GetString();
                if (string.IsNullOrWhiteSpace(numberId))
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:29";
                    result.Message = "Invalid number id found in numbers list.";
                    return result;
                }

                var numberData = businessNumbers.Find(x => x.Id == numberId);
                if (numberData == null)
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:29";
                    result.Message = "Number with id " + numberId + " not found in business numbers list.";
                    return result;
                }

                if (postType == "new")
                {
                    if (numberData.RouteId != null)
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:29";
                        result.Message = "Number with id " + numberId + " already has a route.";
                        return result;
                    }
                }
                else
                {
                    if (numberData.RouteId != null && numberData.RouteId != existingRouteData.Id)
                    {
                        result.Code = "AddOrUpdateUserBusinessRoute:29";
                        result.Message = "Number with id " + numberId + " already has a route.";
                        return result;
                    }
                }

                newBusinessAppRouteData.Numbers.Add(numberId);
            }

            // Agent Tab
            if (!changes.RootElement.TryGetProperty("agent", out var agentTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:30";
                result.Message = "Agent tab not found.";
                return result;
            }

            if (!agentTabRootElement.TryGetProperty("selectedAgentId", out var selectedAgentIdProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:31";
                result.Message = "Selected agent id not found.";
                return result;
            }
            string? selectedAgentId = selectedAgentIdProperty.GetString();
            if (string.IsNullOrWhiteSpace(selectedAgentId))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:32";
                result.Message = "Selected agent id is required.";
                return result;
            }
            var getBusinessAgent = await _businessAppRepository.GetAgentById(businessId, selectedAgentId);
            if (getBusinessAgent == null)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:32";
                result.Message = "Selected agent not found.";
                return result;
            }
            newBusinessAppRouteData.Agent.SelectedAgentId = selectedAgentId;

            if (!agentTabRootElement.TryGetProperty("openingScriptId", out var openingScriptIdProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:33";
                result.Message = "Opening script id not found.";
                return result;
            }
            string? openingScriptId = openingScriptIdProperty.GetString();
            if (string.IsNullOrWhiteSpace(openingScriptId))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:34";
                result.Message = "Opening script id is required.";
                return result;
            }
            if (getBusinessAgent.Scripts.Find(x => x.Id == openingScriptId) == null)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:34";
                result.Message = "Opening script not found within selected agent.";
                return result;
            }
            newBusinessAppRouteData.Agent.OpeningScriptId = openingScriptId;

            if (!agentTabRootElement.TryGetProperty("conversationType", out var conversationTypeProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:35";
                result.Message = "Conversation type not found.";
                return result;
            }
            if (!conversationTypeProperty.TryGetInt32(out var conversationType))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:36";
                result.Message = "Invalid conversation type value.";
                return result;
            }
            if (!Enum.IsDefined(typeof(AgentConversationTypeENUM), conversationType))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:37";
                result.Message = "Conversation type not found in enum.";
                return result;
            }
            newBusinessAppRouteData.Agent.ConversationType = (AgentConversationTypeENUM)conversationType;

            if (newBusinessAppRouteData.Agent.ConversationType == AgentConversationTypeENUM.Interruptible)
            {
                if (!agentTabRootElement.TryGetProperty("interruptibleConversationTypeWords", out var interruptibleWordsProperty))
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:38";
                    result.Message = "Interruptible conversation type words not found.";
                    return result;
                }
                if (!interruptibleWordsProperty.TryGetInt32(out var interruptibleWords) || interruptibleWords < 1)
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:39";
                    result.Message = "Invalid interruptible conversation type words value. (min 1)";
                    return result;
                }
                newBusinessAppRouteData.Agent.InterruptibleConversationTypeWords = interruptibleWords;
            }

            if (!agentTabRootElement.TryGetProperty("timezones", out var timezonesProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:40";
                result.Message = "Timezones not found.";
                return result;
            }
            foreach (var timezone in timezonesProperty.EnumerateArray())
            {
                string? timezoneValue = timezone.GetString();
                if (string.IsNullOrWhiteSpace(timezoneValue))
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:41";
                    result.Message = "Invalid timezone value found in timezones list.";
                    return result;
                }
                // TODO VALIDATE TIMEZOME
                newBusinessAppRouteData.Agent.Timezones.Add(timezoneValue);
            }

            if (!agentTabRootElement.TryGetProperty("callerNumberInContext", out var callerNumberInContextProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:42";
                result.Message = "Caller number in context not found.";
                return result;
            }
            newBusinessAppRouteData.Agent.CallerNumberInContext = callerNumberInContextProperty.GetBoolean();

            if (!agentTabRootElement.TryGetProperty("routeNumberInContext", out var routeNumberInContextProperty))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:43";
                result.Message = "Route number in context not found.";
                return result;
            }
            newBusinessAppRouteData.Agent.RouteNumberInContext = routeNumberInContextProperty.GetBoolean();

            // Actions Tab
            if (!changes.RootElement.TryGetProperty("actions", out var actionsTabRootElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:44";
                result.Message = "Actions tab not found.";
                return result;
            }

            // Validate Ringing Tool
            if (!actionsTabRootElement.TryGetProperty("ringingTool", out var ringingToolElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:45";
                result.Message = "Ringing tool not found.";
                return result;
            }
            var ringingToolValidationResult = await ValidateBusinessRouteActionData(businessId, businessLanguages[0], ringingToolElement, "Ringing");
            if (!ringingToolValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:" + ringingToolValidationResult.Code;
                result.Message = ringingToolValidationResult.Message;
                return result;
            }
            newBusinessAppRouteData.Actions.RingingTool = ringingToolValidationResult.Data;

            // Validate Picked Tool
            if (!actionsTabRootElement.TryGetProperty("callPickedTool", out var pickedToolElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:47";
                result.Message = "Picked tool not found.";
                return result;
            }
            var pickedToolValidationResult = await ValidateBusinessRouteActionData(businessId, businessLanguages[0], pickedToolElement, "Picked");
            if (!pickedToolValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:" + pickedToolValidationResult.Code;
                result.Message = pickedToolValidationResult.Message;
                return result;
            }
            newBusinessAppRouteData.Actions.CallPickedTool = pickedToolValidationResult.Data;

            // Validate Ended Tool
            if (!actionsTabRootElement.TryGetProperty("callEndedTool", out var endedToolElement))
            {
                result.Code = "AddOrUpdateUserBusinessRoute:49";
                result.Message = "Ended tool not found.";
                return result;
            }
            var endedToolValidationResult = await ValidateBusinessRouteActionData(businessId, businessLanguages[0], endedToolElement, "Ended");
            if (!endedToolValidationResult.Success)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:" + endedToolValidationResult.Code;
                result.Message = endedToolValidationResult.Message;
                return result;
            }
            newBusinessAppRouteData.Actions.CallEndedTool = endedToolValidationResult.Data;

            // Save or Update in Database
            if (postType == "new")
            {
                newBusinessAppRouteData.Id = Guid.NewGuid().ToString();
                var addRouteResult = await _businessAppRepository.AddBusinessAppRoute(businessId, newBusinessAppRouteData);
                if (!addRouteResult)
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:51";
                    result.Message = "Failed to add business app route.";
                    return result;
                }
            }
            else
            {
                newBusinessAppRouteData.Id = existingRouteData.Id;
                var updateRouteResult = await _businessAppRepository.UpdateBusinessAppRoute(businessId, newBusinessAppRouteData);
                if (!updateRouteResult)
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:52";
                    result.Message = "Failed to update business app route.";
                    return result;
                }

                foreach (var oldNumberId in existingRouteData.Numbers)
                {
                    if (!newBusinessAppRouteData.Numbers.Contains(oldNumberId))
                    {
                        await _businessAppRepository.UpdateBusinessNumberRoute(businessId, oldNumberId, null);
                    }
                }
            }

            foreach (var numberId in newBusinessAppRouteData.Numbers)
            {
                await _businessAppRepository.UpdateBusinessNumberRoute(businessId, numberId, newBusinessAppRouteData.Id);
            }

            result.Success = true;
            result.Data = newBusinessAppRouteData;
            return result;
        }

        private async Task<FunctionReturnResult<BusinessAppRouteActionTool>> ValidateBusinessRouteActionData(long businessId, string businessDefaultLanguage, JsonElement actionsTabRootElement, string actionType)
        {
            var result = new FunctionReturnResult<BusinessAppRouteActionTool>();
            result.Data = new BusinessAppRouteActionTool();

            if (!actionsTabRootElement.TryGetProperty("selectedToolId", out var selectedToolIdProperty))
            {
                throw new Exception($"{actionType} selected tool id not found.");
            }

            string? selectedToolId = selectedToolIdProperty.GetString();
            if (selectedToolId == null)
            {
                result.Success = true;
                return result;
            }
            var selectedToolData = await _businessAppRepository.GetBusinessAppTool(businessId, selectedToolId);
            if (selectedToolData == null)
            {
                result.Code = "ValidateBusinessRouteActionData:1";
                result.Message = $"{actionType} tool not found in business.";
                return result;
            }
            result.Data.SelectedToolId = selectedToolId;
            result.Data.Arguments = new Dictionary<string, object>();

            if (!actionsTabRootElement.TryGetProperty("arguments", out var argumentsProperty))
            {
                result.Code = "ValidateBusinessRouteActionData:2";
                result.Message = $"{actionType} tool arguments not found.";
                return result;
            }

            foreach (var toolInputArgument in selectedToolData.Configuration.InputSchemea)
            {
                bool foundProperty = argumentsProperty.TryGetProperty(toolInputArgument.Id, out var argumentValueProperty);

                if (!foundProperty && toolInputArgument.IsRequired)
                {
                    result.Code = "ValidateBusinessRouteActionData:3";
                    result.Message = $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} not found but is required.";
                    return result;
                }
                else if (foundProperty)
                {
                    // Handle Array Type
                    if (toolInputArgument.IsArray)
                    {
                        if (argumentValueProperty.ValueKind != JsonValueKind.Array)
                        {
                            result.Code = "ValidateBusinessRouteActionData:4";
                            result.Message = $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} should be an array.";
                            return result;
                        }

                        var arrayValues = new List<object>();
                        foreach (var arrayElement in argumentValueProperty.EnumerateArray())
                        {
                            var validationResult = ValidateArgumentValue(businessDefaultLanguage, arrayElement, toolInputArgument, actionType);
                            if (!validationResult.Success)
                            {
                                result.Code = validationResult.Code;
                                result.Message = validationResult.Message;
                                return result;
                            }
                            arrayValues.Add(validationResult.Data);
                        }

                        if (toolInputArgument.IsRequired && arrayValues.Count == 0)
                        {
                            result.Code = "ValidateBusinessRouteActionData:5";
                            result.Message = $"{actionType} tool input argument {toolInputArgument.Name[businessDefaultLanguage]} array cannot be empty as it is required.";
                            return result;
                        }

                        result.Data.Arguments.Add(toolInputArgument.Id, arrayValues);
                    }
                    // Handle Single Value
                    else
                    {
                        var validationResult = ValidateArgumentValue(businessDefaultLanguage, argumentValueProperty, toolInputArgument, actionType);
                        if (!validationResult.Success)
                        {
                            result.Code = validationResult.Code;
                            result.Message = validationResult.Message;
                            return result;
                        }
                        result.Data.Arguments.Add(toolInputArgument.Id, validationResult.Data);
                    }
                }
            }

            result.Success = true;
            return result;
        }

        // move to a tool helper class/function
        private FunctionReturnResult<object> ValidateArgumentValue(string businessDefaultLanguage, JsonElement value, BusinessAppToolConfigurationInputSchemea argument, string actionType)
        {
            var result = new FunctionReturnResult<object>();

            switch (argument.Type)
            {
                case BusinessAppToolConfigurationInputSchemeaTypeEnum.String:
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "ValidateArgumentValue:1";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected string.";
                        return result;
                    }

                    string? stringValue = value.GetString();
                    if (string.IsNullOrWhiteSpace(stringValue))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:2";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = stringValue;
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Number:
                    if (value.ValueKind != JsonValueKind.Number && argument.IsRequired)
                    {
                        result.Code = "ValidateArgumentValue:3";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected number.";
                        return result;
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        result.Data = string.Empty;
                        break;
                    }

                    if (!value.TryGetDouble(out var numberValue))
                    {
                        result.Code = "ValidateArgumentValue:4";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value type mismatch.";
                        return result;
                    }

                    result.Data = numberValue;
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.Boolean:
                    if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False && argument.IsRequired)
                    {
                        result.Code = "ValidateArgumentValue:5";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected boolean.";
                        return result;
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = value.GetBoolean();
                    break;

                case BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime:
                    if (value.ValueKind != JsonValueKind.String)
                    {
                        result.Code = "ValidateArgumentValue:6";
                        result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} type mismatch, expected date time string.";
                        return result;
                    }

                    string? dateTimeString = value.GetString();
                    if (string.IsNullOrWhiteSpace(dateTimeString))
                    {
                        if (argument.IsRequired)
                        {
                            result.Code = "ValidateArgumentValue:7";
                            result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} value is empty but it is required.";
                            return result;
                        }
                        result.Data = string.Empty;
                        break;
                    }

                    result.Data = dateTimeString;
                    break;

                default:
                    result.Code = "ValidateArgumentValue:9";
                    result.Message = $"{actionType} tool input argument {argument.Name[businessDefaultLanguage]} has unknown type.";
                    return result;
            }

            result.Success = true;
            return result;
        }
    }
}

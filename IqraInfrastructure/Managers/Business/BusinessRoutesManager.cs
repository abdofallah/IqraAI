using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Utilities;
using IqraInfrastructure.Helpers.Business;
using IqraInfrastructure.Repositories.Business;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessRoutesManager
    {
        private readonly BusinessManager _parentBusinessManager;
        private readonly IMongoClient _mongoClient;

        private readonly BusinessAppRepository _businessAppRepository;
        private readonly BusinessRepository _businessRepository;
        private readonly IntegrationConfigurationManager _integrationConfigurationManager;

        public BusinessRoutesManager(BusinessManager businessManager, IMongoClient mongoClient, BusinessAppRepository businessAppRepository, BusinessRepository businessRepository, IntegrationConfigurationManager integrationConfigurationManager)
        {
            _parentBusinessManager = businessManager;
            _mongoClient = mongoClient;

            _businessAppRepository = businessAppRepository;
            _businessRepository = businessRepository;
            _integrationConfigurationManager = integrationConfigurationManager;
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
            if (!maxCallTimeSProperty.TryGetInt32(out var maxCallTimeS) || maxCallTimeS < 0 || maxCallTimeS > 1800) // this is also used in BusinessMakeCall so we should make this a const
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
            var businessAgentExists = await _businessAppRepository.CheckAgentExists(businessId, selectedAgentId);
            if (!businessAgentExists)
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
            var businessScriptExists = await _businessAppRepository.CheckScriptExists(businessId, openingScriptId);
            if (!businessScriptExists)
            {
                result.Code = "AddOrUpdateUserBusinessRoute:34";
                result.Message = "Opening script not found.";
                return result;
            }
            newBusinessAppRouteData.Agent.OpeningScriptId = openingScriptId;
   
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
                if (!TimeZoneHelper.ValidateOffsetString(timezoneValue))
                {
                    result.Code = "AddOrUpdateUserBusinessRoute:41.1";
                    result.Message = $"Unable to validate timezone {timezoneValue}.";
                    return result;
                }
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

            using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction();
                try
                {
                    // Save or Update in Database
                    if (postType == "new")
                    {
                        newBusinessAppRouteData.Id = ObjectId.GenerateNewId().ToString();
                        var addRouteResult = await _businessAppRepository.AddBusinessAppRoute(businessId, newBusinessAppRouteData, session);
                        if (!addRouteResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateUserBusinessRoute:DB_ADD_FAILED",
                                "Failed to add business app route."
                            );
                        }
                    }
                    else
                    {
                        newBusinessAppRouteData.Id = existingRouteData!.Id;
                        var updateRouteResult = await _businessAppRepository.UpdateBusinessAppRoute(businessId, newBusinessAppRouteData, session);
                        if (!updateRouteResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateUserBusinessRoute:DB_UPDATE_FAILED",
                                "Failed to update business app route."
                            );
                        }

                        // Cleanup: Removed Numbers
                        foreach (var oldNumberId in existingRouteData.Numbers)
                        {
                            if (!newBusinessAppRouteData.Numbers.Contains(oldNumberId))
                            {
                                var removeNumberRouteResult = await _businessAppRepository.UpdateBusinessNumberRoute(businessId, oldNumberId, null, session);
                                if (!removeNumberRouteResult)
                                {
                                    await session.AbortTransactionAsync();
                                    return result.SetFailureResult(
                                        "AddOrUpdateUserBusinessRoute:NUMBER_ROUTE_REMOVAL_FAILED",
                                        "Failed to remove previous number route."
                                    );
                                }
                            }
                        }

                        // Cleanup: Changed Agent
                        if (existingRouteData.Agent.SelectedAgentId != newBusinessAppRouteData.Agent.SelectedAgentId)
                        {
                            var removeAgentRouteReferenceResult = await _businessAppRepository.RemoveInboundRoutingReferenceFromAgent(businessId, existingRouteData.Agent.SelectedAgentId, newBusinessAppRouteData.Id, session);
                            if (!removeAgentRouteReferenceResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateUserBusinessRoute:AGENT_ROUTE_REMOVAL_FAILED",
                                    "Failed to remove agent route reference."
                                );
                            }
                        }

                        // Cleanup: Changed Script
                        if (existingRouteData.Agent.OpeningScriptId != newBusinessAppRouteData.Agent.OpeningScriptId)
                        {
                            var removeScriptRouteReferenceResult = await _businessAppRepository.RemoveInboundRoutingReferenceFromScript(businessId, existingRouteData.Agent.OpeningScriptId, newBusinessAppRouteData.Id, session);
                            if (!removeScriptRouteReferenceResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateUserBusinessRoute:SCRIPT_ROUTE_REMOVAL_FAILED",
                                    "Failed to remove script route reference."
                                );
                            }
                        }

                        // Cleanup: Changed Post Analysis
                        if (
                            existingRouteData.PostAnalysis.PostAnalysisId != null &&
                            existingRouteData.PostAnalysis.PostAnalysisId != newBusinessAppRouteData.PostAnalysis.PostAnalysisId
                        ) {
                            var removePostAnalysisRouteReferenceResult = await _businessAppRepository.RemoveInboundRoutingReferenceFromPostAnalysis(businessId, existingRouteData.PostAnalysis.PostAnalysisId, newBusinessAppRouteData.Id, session);
                            if (!removePostAnalysisRouteReferenceResult)
                            {
                                await session.AbortTransactionAsync();
                                return result.SetFailureResult(
                                    "AddOrUpdateUserBusinessRoute:POST_ANALYSIS_ROUTE_REMOVAL_FAILED",
                                    "Failed to remove post analysis route reference."
                                );
                            }
                        }
                    }

                    // Update Numbers Reference
                    foreach (var numberId in newBusinessAppRouteData.Numbers)
                    {
                        var updateNumberRouteResult = await _businessAppRepository.UpdateBusinessNumberRoute(businessId, numberId, newBusinessAppRouteData.Id, session);
                        if (!updateNumberRouteResult)
                        {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateUserBusinessRoute:NUMBER_ROUTE_UPDATE_FAILED",
                                "Failed to update number route."
                            );
                        }
                    }

                    // Update Agent Reference
                    var addAgentRouteReferenceResult = await _businessAppRepository.AddInboundRoutingReferenceToAgent(businessId, newBusinessAppRouteData.Agent.SelectedAgentId, newBusinessAppRouteData.Id, session);
                    if (!addAgentRouteReferenceResult)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateUserBusinessRoute:AGENT_ROUTE_ADD_FAILED",
                            "Failed to add agent route reference."
                        );
                    }

                    // Update Script Reference
                    var addScriptRouteReferenceResult = await _businessAppRepository.AddInboundRoutingReferenceToScript(businessId, newBusinessAppRouteData.Agent.OpeningScriptId, newBusinessAppRouteData.Id, session);
                    if (!addScriptRouteReferenceResult)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateUserBusinessRoute:SCRIPT_ROUTE_ADD_FAILED",
                            "Failed to add script route reference."
                        );
                    }

                    // Update Post Analysis Reference
                    if (newBusinessAppRouteData.PostAnalysis.PostAnalysisId != null)
                    {
                        var addPostAnalysisReferenceResult = await _businessAppRepository.AddInboundRoutingReferenceToPostAnalysis(businessId, newBusinessAppRouteData.PostAnalysis.PostAnalysisId, newBusinessAppRouteData.Id, session);
                        if (!addPostAnalysisReferenceResult) {
                            await session.AbortTransactionAsync();
                            return result.SetFailureResult(
                                "AddOrUpdateUserBusinessRoute:POST_ANALYSIS_ROUTE_ADD_FAILED",
                                "Failed to add post analysis route reference."
                            );
                        }
                    }

                    // Update Tool References
                    try
                    {
                        await UpdateRouteToolReferences(businessId, newBusinessAppRouteData.Id, existingRouteData, newBusinessAppRouteData, session);
                    }
                    catch (Exception ex)
                    {
                        await session.AbortTransactionAsync();
                        return result.SetFailureResult(
                            "AddOrUpdateUserBusinessRoute:TOOL_REF_UPDATE_FAILED",
                            $"Failed to update tool references: {ex.Message}"
                        );
                    }

                    await session.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await session.AbortTransactionAsync();
                    return result.SetFailureResult(
                        "AddOrUpdateUserBusinessRoute:DB_EXCEPTION",
                        $"An error occurred while adding or updating user business route: {ex.Message}"
                    );
                }
            }

            return result.SetSuccessResult(newBusinessAppRouteData);
        }

        private async Task UpdateRouteToolReferences(
            long businessId,
            string routeId,
            BusinessAppRoute? oldRoute,
            BusinessAppRoute newRoute,
            IClientSessionHandle session
        ) {
            // Helper to diff single tool reference
            async Task HandleToolRef(
                string? oldToolId,
                string? newToolId,
                BusinessAppToolInboundRouteActionType actionType)
            {
                // Remove Old
                if (!string.IsNullOrEmpty(oldToolId) && oldToolId != newToolId)
                {
                    var refObj = new BusinessAppToolInboundRouteReference { RouteId = routeId, ActionType = actionType };
                    if (!await _businessAppRepository.RemoveToolInboundRouteReference(businessId, oldToolId, refObj, session))
                    {
                        throw new Exception($"Failed to remove {actionType} tool reference from tool {oldToolId}");
                    }
                }

                // Add New
                if (!string.IsNullOrEmpty(newToolId))
                {
                    var refObj = new BusinessAppToolInboundRouteReference { RouteId = routeId, ActionType = actionType };
                    if (!await _businessAppRepository.AddToolInboundRouteReference(businessId, newToolId, refObj, session))
                    {
                        throw new Exception($"Failed to add {actionType} tool reference to tool {newToolId}");
                    }
                }
            }

            // 1. Ringing Tool
            await HandleToolRef(
                oldRoute?.Actions.RingingTool.SelectedToolId,
                newRoute.Actions.RingingTool.SelectedToolId,
                BusinessAppToolInboundRouteActionType.Ringing
            );

            // 2. Call Picked Tool
            await HandleToolRef(
                oldRoute?.Actions.CallPickedTool.SelectedToolId,
                newRoute.Actions.CallPickedTool.SelectedToolId,
                BusinessAppToolInboundRouteActionType.CallPicked
            );

            // 3. Call Ended Tool
            await HandleToolRef(
                oldRoute?.Actions.CallEndedTool.SelectedToolId,
                newRoute.Actions.CallEndedTool.SelectedToolId,
                BusinessAppToolInboundRouteActionType.CallEnded
            );
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
                            var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, arrayElement, toolInputArgument, actionType);
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
                        var validationResult = BusinessAppToolPropertyValidator.ValidateArgumentValue(businessDefaultLanguage, argumentValueProperty, toolInputArgument, actionType);
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
    }
}

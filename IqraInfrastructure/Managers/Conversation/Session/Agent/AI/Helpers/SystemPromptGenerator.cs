using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation.Context;
using IqraCore.Entities.Helper;
using IqraCore.Entities.Helper.Agent;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using System.Globalization;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers
{
    public class SystemPromptGenerator
    {
        private readonly ILogger<SystemPromptGenerator> _logger;
        private readonly LanguagesManager _languagesManager;
        private readonly LLMProviderManager _llmProviderManager;

        public SystemPromptGenerator(ILogger<SystemPromptGenerator> logger, LanguagesManager languagesManager, LLMProviderManager llmProviderManager)
        {
            _logger = logger;
            _languagesManager = languagesManager;
            _llmProviderManager = llmProviderManager;
        }

        public async Task<FunctionReturnResult<string?>> GenerateInitialSystemPrompt(
            BusinessApp businessApp,
            BusinessAppAgent agent,
            ConversationSessionContext currentSessionContext,
            string languageCode,
            InterfaceLLMProviderEnum llmProvider,
            string llmModelId
        )
        {
            var result = new FunctionReturnResult<string?>();

            try
            {
                var langaugeDataResult = await _languagesManager.GetLanguageByCode(languageCode);
                if (!langaugeDataResult.Success)
                {
                    result.Code = "GenerateSystemPrompt:" + langaugeDataResult.Code;
                    result.Message = langaugeDataResult.Message;
                    return result;
                }

                var llmProviderDataResult = await _llmProviderManager.GetProviderData(llmProvider);
                if (llmProviderDataResult == null)
                {
                    result.Code = "GenerateSystemPrompt:1";
                    result.Message = "LLM provider not found";
                    return result;
                }

                var llmModelData = llmProviderDataResult.Models.Find(m => m.Id == llmModelId);
                if (llmModelData == null)
                {
                    result.Code = "GenerateSystemPrompt:2";
                    result.Message = "LLM model not found";
                    return result;
                }

                if (!llmModelData.PromptTemplates.TryGetValue(languageCode, out string? systemPromptForLanguage) || string.IsNullOrWhiteSpace(systemPromptForLanguage))
                {
                    result.Code = "GenerateSystemPrompt:3";
                    result.Message = "System prompt not found for language or is empty";
                    return result;
                }

                BusinessAppAgentScript? openingAgentScript = agent.Scripts.Find(d => d.Id == currentSessionContext.Agent.OpeningScriptId);
                if (openingAgentScript == null)
                {
                    result.Code = "GenerateSystemPrompt:4";
                    result.Message = "Opening agent script not found";
                    return result;
                }

                var openingAgentScriptNodesData = GetScriptNodesData(openingAgentScript, businessApp, agent);

                // Initialize Scriban template
                var template = Template.Parse(systemPromptForLanguage);    
                if (template.HasErrors)
                {
                    result.Code = "GenerateSystemPrompt:5";
                    result.Message = "Error parsing system prompt template: " + string.Join(", ", template.Messages);
                    return result;
                }

                // Create template context
                var templateContext = new TemplateContext();
                var scriptObject = new ScriptObject();
                
                // Setup model with localized data
                var modelObject = new ScriptObject();
                
                // TODO compile all these objects at once using Task.WhenAll (compare performance with and without)

                // Add Agent data
                var agentObject = new ScriptObject();
                agentObject["Personality"] = CreateAgentPersonalityObject(agent.Personality, languageCode);
                agentObject["Context"] = CreateAgentContextObject(agent.Context);
                agentObject["Scripts"] = CreateAgentScriptsObject(openingAgentScriptNodesData.tools, new List<BusinessAppAgentScript>() { openingAgentScript }, languageCode);
                agentObject["ScriptTools"] = CreateAgentScriptToolsObject(openingAgentScriptNodesData.tools, languageCode);
                agentObject["ScriptAgents"] = CreateAgentScriptAgentsObject(openingAgentScriptNodesData.agents, languageCode);
                agentObject["ScriptAddableScripts"] = CreateAgentScriptAddableScriptsObject(openingAgentScriptNodesData.scripts, languageCode);
                agentObject["HasDTMFRequestTool"] = openingAgentScriptNodesData.hasDTMFRequestTool;
                agentObject["HasSendSMSTool"] = openingAgentScriptNodesData.hasSendSMSTool;
                agentObject["HasRetrieveKnowledgeBaseTool"] = openingAgentScriptNodesData.hasRetrieveKnowledgeBaseTool;
                modelObject["Agent"] = agentObject;
                
                // Add Context (company) data
                var contextObject = new ScriptObject();
                contextObject["Branding"] = CreateBrandingObject(businessApp.Context.Branding, languageCode);
                contextObject["Branches"] = CreateBranchesObject(businessApp.Context.Branches, languageCode);
                contextObject["Services"] = CreateServicesObject(businessApp.Context.Services, languageCode);
                contextObject["Products"] = CreateProductsObject(businessApp.Context.Products, languageCode);
                modelObject["Context"] = contextObject;

                // Add Route Data
                var routeObject = new ScriptObject();
                routeObject["Multilangauges"] = CreateRouteLanguageObject(currentSessionContext.Language, languageCode);
                modelObject["Route"] = routeObject;

                // Add the model to the context
                scriptObject.Import(modelObject);
                templateContext.PushGlobal(scriptObject);

                // Render the template
                var renderedPrompt = await template.RenderAsync(templateContext);
                if (string.IsNullOrWhiteSpace(renderedPrompt))
                {
                    result.Code = "GenerateSystemPrompt:6";
                    result.Message = "System prompt is empty after rendering";
                    return result;
                }

                result.Success = true;
                result.Data = renderedPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating system prompt");
                result.Code = "GenerateSystemPrompt:-1";
                result.Message = "Error generating system prompt: " + ex.Message;
            }

            return result;
        }

        public async Task<FunctionReturnResult<string?>> GenerateDateTimeInformationForMessage(string? dateTimeFormatTemplate, List<string> timezones)
        {
            var result = new FunctionReturnResult<string?>();

            if (string.IsNullOrEmpty(dateTimeFormatTemplate))
            {
                dateTimeFormatTemplate = "<DateTimeInformation>Date and Time right now for timezone ({{Session.Route.Agent.Timezone.Name}}) is: {{Session.Route.Agent.Timezone.Time}}</DateTimeInformation>";
            }

            // Initialize Scriban template
            var template = Template.Parse(dateTimeFormatTemplate);
            if (template.HasErrors)
            {
                result.Code = "GenerateDateTimeInformationForMessage:1";
                result.Message = "Error parsing system prompt template: " + string.Join(", ", template.Messages);
                return result;
            }

            // Create template context
            var templateContext = new TemplateContext();
            var modelObject = new ScriptObject();

            // Add Session data
            var sessionObject = new ScriptObject();

            // Add Route data
            var routeObject = new ScriptObject();
            routeObject["Agent"] = CreateRouteAgentTimezoneObject(timezones);
            sessionObject["Route"] = routeObject;
            modelObject["Session"] = sessionObject;

            // Add the model to the context
            modelObject.Import(modelObject);
            templateContext.PushGlobal(modelObject);

            // Render the template
            var renderedPrompt = await template.RenderAsync(templateContext);
            if (string.IsNullOrWhiteSpace(renderedPrompt))
            {
                result.Code = "GenerateDateTimeInformationForMessage:2";
                result.Message = "System prompt is empty after rendering";
                return result;
            }

            result.Success = true;
            result.Data = renderedPrompt;

            return result;
        }

        public async Task<FunctionReturnResult<string?>> FillSessionInformationInPrompt(string? sessionInformationTemplate, ConversationSessionContext currentSessionContext, BusinessAppAgent routeAgent, string languageCode)
        {
            var result = new FunctionReturnResult<string?>();

            // todo move this out of here into the dedicated backend prompt management system
            if (string.IsNullOrEmpty(sessionInformationTemplate))
            {
                sessionInformationTemplate = @"
                Here is the session information that will be helpful for your context:
                <SessionInformation>
                    Current Choosen Language: {{Session.Route.Multilangauges.CurrentLanguage.Code}} | {{Session.Route.Multilangauges.CurrentLanguage.Name}}
                    {{~ if Session.Route.Multilangauges.Enabled ~}}
                        Available Languages:
                        {{ for language in Session.Route.Multilangauges.Languages }}
                            - {{language.Code}} | {{language.Name}}
                        {{~ end ~}}
                    {{~ end ~}}	
	                {{~ if Session.Route.Agent.CallerNumberInContext ~}}
	                Caller phone Number is: {{Session.Caller.PhoneNumber}}
	                {{~ end ~}}

                    {{~ if Session.DynamicVariables.size > 0 ~}}
                    <DynamicVariables>
                        {{~ for dynamicVariable in Session.DynamicVariables ~}}
                            - {{dynamicVariable}}
                        {{~ end ~}}
                    </DynamicVariables>
                    {{~ end ~}}
                </SessionInformation>";
            }
            
            // Initialize Scriban template
            var template = Template.Parse(sessionInformationTemplate);
            if (template.HasErrors)
            {
                result.Code = "FillSessionInformationInPrompt:1";
                result.Message = "Error parsing system prompt template: " + string.Join(", ", template.Messages);
                return result;
            }

            // Create template context
            var templateContext = new TemplateContext();
            var modelObject = new ScriptObject();

            // Add Session data
            var sessionObject = new ScriptObject();
            var callerObject = new ScriptObject();
            sessionObject["Caller"] = callerObject;
            modelObject["Session"] = sessionObject;

            // Add Route data
            var routeObject = new ScriptObject();
            routeObject["Agent"] = CreateRouteAgentObject(currentSessionContext.Agent);
            routeObject["Multilangauges"] = await CreateRouteLanguageObject(currentSessionContext.Language, languageCode);
            sessionObject["Route"] = routeObject;
            sessionObject["DynamicVariables"] = CreateSessionDynamicVariablesObject(currentSessionContext);

            // Add the model to the context
            modelObject.Import(modelObject);
            templateContext.PushGlobal(modelObject);

            // Render the template
            var renderedPrompt = await template.RenderAsync(templateContext);
            if (string.IsNullOrWhiteSpace(renderedPrompt))
            {
                result.Code = "FillSessionInformationInPrompt:2";
                result.Message = "System prompt is empty after rendering";
                return result;
            }

            result.Success = true;
            result.Data = renderedPrompt;

            return result;
        }

        private ScriptArray CreateSessionDynamicVariablesObject(ConversationSessionContext context)
        {
            var dynamicVariablesObject = new ScriptArray();
            if (context.DynamicVariables == null || context.DynamicVariables.Count == 0) return dynamicVariablesObject;

            foreach (var dynamicVariable in context.DynamicVariables)
            {
                dynamicVariablesObject.Add($"{dynamicVariable.Key}: {dynamicVariable.Value}") ;
            }
            return dynamicVariablesObject;
        }

        private ScriptObject CreateAgentPersonalityObject(BusinessAppAgentPersonality personality, string languageCode)
        {
            var personalityObject = new ScriptObject();
            personalityObject["Name"] = GetLocalizedString(personality.Name, languageCode, "AI Assistant");
            personalityObject["Role"] = GetLocalizedString(personality.Role, languageCode, "Customer Support Agent");
            personalityObject["Capabilities"] = GetLocalizedList(personality.Capabilities, languageCode);
            personalityObject["Ethics"] = GetLocalizedList(personality.Ethics, languageCode);
            personalityObject["Tone"] = GetLocalizedList(personality.Tone, languageCode);
            return personalityObject;
        }

        private ScriptObject CreateAgentContextObject(BusinessAppAgentContext context)
        {
            var contextObject = new ScriptObject();
            contextObject["UseBranding"] = context.UseBranding;
            contextObject["UseBranches"] = context.UseBranches;
            contextObject["UseServices"] = context.UseServices;
            contextObject["UseProducts"] = context.UseProducts;
            return contextObject;
        }

        private ScriptArray CreateAgentScriptsObject(List<BusinessAppTool> scriptCustomTools, List<BusinessAppAgentScript> scripts, string languageCode)
        {
            var scriptsArray = new ScriptArray();
            if (scripts == null || !scripts.Any())
            {
                return scriptsArray;
            }

            foreach (var script in scripts)
            {
                var scriptObject = new ScriptObject();
                scriptObject["Id"] = script.Id;
                scriptObject["Name"] = GetLocalizedString(script.General.Name, languageCode, "");
                scriptObject["Description"] = GetLocalizedString(script.General.Description, languageCode, "");

                ConversationFlowMDBuilderHelper scriptBuilder = new ConversationFlowMDBuilderHelper(script, languageCode, scriptCustomTools);
                scriptObject["ConversationFlow"] = scriptBuilder.ConvertScriptToHumanReadable();

                scriptsArray.Add(scriptObject);
            }

            return scriptsArray;
        }

        // Helper class to represent an edge in the script graph  

        private ScriptArray CreateAgentScriptToolsObject(List<BusinessAppTool> tools, string languageCode)
        {
            var toolsArray = new ScriptArray();
            if (tools != null)
            {
                foreach (var tool in tools)
                {
                    var toolObject = CreateAgentScriptCustomToolObject(tool, languageCode);

                    toolsArray.Add(toolObject);
                }
            }
            return toolsArray;
        }
        private ScriptObject CreateAgentScriptCustomToolObject(BusinessAppTool tool, string languageCode)
        {
            var toolObject = new ScriptObject();

            // Basic tool information
            toolObject["Id"] = tool.Id;
            toolObject["Name"] = GetLocalizedString(tool.General.Name, languageCode, "Tool");
            toolObject["Description"] = GetLocalizedString(tool.General.ShortDescription, languageCode, "");

            // Input schemas
            var inputSchemasArray = new ScriptArray();
            if (tool.Configuration.InputSchemea != null)
            {
                foreach (var inputSchema in tool.Configuration.InputSchemea)
                {
                    var schemaObject = new ScriptObject();
                    schemaObject["Id"] = inputSchema.Id;
                    schemaObject["Name"] = GetLocalizedString(inputSchema.Name, languageCode, "Input");
                    schemaObject["Description"] = GetLocalizedString(inputSchema.Description, languageCode, "");
                    schemaObject["Type"] = inputSchema.Type.ToString();
                    schemaObject["IsArray"] = inputSchema.IsArray;
                    schemaObject["IsRequired"] = inputSchema.IsRequired;

                    if (inputSchema.Type == BusinessAppToolConfigurationInputSchemeaTypeEnum.DateTime)
                    {
                        schemaObject["DateTimeFormat"] = ((BusinessAppToolConfigurationInputSchemeaDateTime)inputSchema).DateTimeFormat;
                    }

                    inputSchemasArray.Add(schemaObject);
                }
            }
            toolObject["InputSchemeas"] = inputSchemasArray;

            // Response information
            var responsesArray = new ScriptArray();
            if (tool.Response != null)
            {
                foreach (var response in tool.Response)
                {
                    var responseObject = new ScriptObject();
                    responseObject["Type"] = response.Key;

                    // Add response details
                    if (response.Value != null)
                    {
                        // Static response if available
                        if (response.Value.HasStaticResponse)
                        {
                            responseObject["StaticResponse"] = GetLocalizedString(response.Value.StaticResponse, languageCode, null);
                        }
                        else
                        {
                            responseObject["StaticResponse"] = null;
                        }
                    }

                    responsesArray.Add(responseObject);
                }
            }
            toolObject["Responses"] = responsesArray;

            return toolObject;
        }

        private ScriptArray CreateAgentScriptAgentsObject(List<BusinessAppAgent> agents, string languageCode)
        {
            var agentsArray = new ScriptArray();
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    var agentObject = new ScriptObject();

                    // Basic tool information
                    agentObject["Id"] = agent.Id;
                    agentObject["Name"] = GetLocalizedString(agent.General.Name, languageCode, "Tool");
                    agentObject["Description"] = GetLocalizedString(agent.General.Description, languageCode, ""); 
                }
            }
            return agentsArray;
        }

        private ScriptArray CreateAgentScriptAddableScriptsObject(List<BusinessAppAgentScript> scripts, string languageCode)
        {
            var scriptsArray = new ScriptArray();
            if (scripts != null)
            {
                foreach (var script in scripts)
                {
                    var scriptObject = new ScriptObject();

                    // Basic tool information
                    scriptObject["Id"] = script.Id;
                    scriptObject["Name"] = GetLocalizedString(script.General.Name, languageCode, "Tool");
                    scriptObject["Description"] = GetLocalizedString(script.General.Description, languageCode, ""); 
                }
            }

            return scriptsArray;
        }

        private ScriptObject CreateBrandingObject(BusinessAppContextBranding branding, string languageCode)
        {
            var brandingObject = new ScriptObject();
            brandingObject["Name"] = GetLocalizedString(branding.Name, languageCode, "Company");
            brandingObject["Country"] = GetLocalizedString(branding.Country, languageCode, "");
            brandingObject["GlobalContactEmail"] = GetLocalizedString(branding.Email, languageCode, "");
            brandingObject["GlobalContactPhone"] = GetLocalizedString(branding.Phone, languageCode, "");
            brandingObject["GlobalWebsite"] = GetLocalizedString(branding.Website, languageCode, "");
            
            // Add additional brand information from OtherInformation dictionary
            var brandInfo = new StringBuilder();
            var otherInfo = GetLocalizedDictionary(branding.OtherInformation, languageCode);
            foreach (var info in otherInfo)
            {
                brandInfo.AppendLine();
                brandInfo.AppendLine($"{info.Key}: {info.Value}");
            }
            brandingObject["BrandInformation"] = brandInfo.ToString();
            
            return brandingObject;
        }

        private ScriptArray CreateBranchesObject(List<BusinessAppContextBranch> branches, string languageCode)
        {
            var branchesArray = new ScriptArray();
            if (branches != null)
            {
                foreach (var branch in branches)
                {
                    var branchObject = new ScriptObject();
                    branchObject["Id"] = branch.Id;
                    branchObject["Name"] = GetLocalizedString(branch.General.Name, languageCode, "Branch");
                    branchObject["Address"] = GetLocalizedString(branch.General.Address, languageCode, "");
                    branchObject["Phone"] = GetLocalizedString(branch.General.Phone, languageCode, "");
                    branchObject["Email"] = GetLocalizedString(branch.General.Email, languageCode, "");
                    branchObject["Website"] = GetLocalizedString(branch.General.Website, languageCode, "");
                    
                    // Add additional branch information
                    var branchInfo = new StringBuilder();
                    var otherInfo = GetLocalizedDictionary(branch.General.OtherInformation, languageCode);
                    foreach (var info in otherInfo)
                    {
                        branchInfo.AppendLine();
                        branchInfo.AppendLine($"{info.Key}: {info.Value}");
                    }
                    branchObject["BranchInformation"] = branchInfo.ToString();
                    
                    // Add working hours
                    var workingHoursArray = new ScriptArray();
                    foreach (var workingHourDay in branch.WorkingHours)
                    {
                        var workingHourObject = new ScriptObject();
                        workingHourObject["Name"] = Enum.Parse(typeof(AllDaysOfWeekEnum), workingHourDay.Key).ToString();
                        workingHourObject["IsClosed"] = workingHourDay.Value.IsClosed;
                        
                        // Format timings
                        if (!workingHourDay.Value.IsClosed && workingHourDay.Value.Timings.Count > 0)
                        {
                            var timings = new StringBuilder();
                            foreach (var (start, end) in workingHourDay.Value.Timings)
                            {
                                if (timings.Length > 0) timings.Append(", ");
                                timings.Append($"{start.ToString("HH:mm")} - {end.ToString("HH:mm")}");
                            }
                            workingHourObject["Timings"] = timings.ToString();
                        }
                        else
                        {
                            workingHourObject["Timings"] = "";
                        }
                        
                        workingHoursArray.Add(workingHourObject);
                    }
                    branchObject["WorkingHours"] = workingHoursArray;
                    
                    // Add team members
                    var teamArray = new ScriptArray();
                    if (branch.Team != null)
                    {
                        foreach (var member in branch.Team)
                        {
                            var memberObject = new ScriptObject();
                            memberObject["Name"] = GetLocalizedString(member.Name, languageCode, "Team Member");
                            memberObject["Role"] = GetLocalizedString(member.Role, languageCode, "");
                            memberObject["Email"] = GetLocalizedString(member.Email, languageCode, null);
                            memberObject["Phone"] = GetLocalizedString(member.Phone, languageCode, null);
                            memberObject["Information"] = GetLocalizedString(member.Information, languageCode, null);
                            teamArray.Add(memberObject);
                        }
                    }
                    branchObject["Team"] = teamArray;
                    
                    branchesArray.Add(branchObject);
                }
            }
            return branchesArray;
        }

        private ScriptArray CreateServicesObject(List<BusinessAppContextService> services, string languageCode)
        {
            var servicesArray = new ScriptArray();
            if (services != null)
            {
                foreach (var service in services)
                {
                    var serviceObject = new ScriptObject();
                    serviceObject["Id"] = service.Id;
                    serviceObject["Name"] = GetLocalizedString(service.Name, languageCode, "Service");
                    serviceObject["ShortDescription"] = GetLocalizedString(service.ShortDescription, languageCode, "");

                    // Add other information
                    var otherInfo = GetLocalizedDictionary(service.OtherInformation, languageCode);
                    if (otherInfo.Count != 0)
                    {
                        var infoObject = new ScriptObject();
                        foreach (var info in otherInfo)
                        {
                            infoObject[info.Key] = info.Value;
                        }
                        serviceObject["OtherInformation"] = infoObject;
                    }
                    
                    
                    servicesArray.Add(serviceObject);
                }
            }
            return servicesArray;
        }

        private ScriptArray CreateProductsObject(List<BusinessAppContextProduct> products, string languageCode)
        {
            var productsArray = new ScriptArray();
            if (products != null)
            {
                foreach (var product in products)
                {
                    var productObject = new ScriptObject();
                    productObject["Id"] = product.Id;
                    productObject["Name"] = GetLocalizedString(product.Name, languageCode, "Product");
                    productObject["ShortDescription"] = GetLocalizedString(product.ShortDescription, languageCode, "");
                    
                    // Add other information
                    var otherInfo = GetLocalizedDictionary(product.OtherInformation, languageCode);
                    var infoObject = new ScriptObject();
                    foreach (var info in otherInfo)
                    {
                        infoObject[info.Key] = info.Value;
                    }
                    productObject["OtherInformation"] = infoObject;
                    
                    productsArray.Add(productObject);
                }
            }
            return productsArray;
        }

        private ScriptObject CreateRouteAgentTimezoneObject(List<string> timezones)
        {
            var routeAgentObject = new ScriptObject();

            ScriptObject timezoneObject = new ScriptObject();
            if (timezones != null && timezones.Count > 0)
            {
                var timeZoneOffsetString = timezones[0];

                TimeSpan? offset = TimeZoneHelper.ParseOffsetString(timeZoneOffsetString);
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                DateTimeOffset targetTime;

                if (offset != null)
                {
                    targetTime = utcNow.ToOffset(offset.Value);
                }
                else
                {
                    targetTime = utcNow;
                    timeZoneOffsetString = "00:00";
                }

                string formattedTime = targetTime.ToString("h:mm:ss tt, dddd, MMMM d, yyyy");

                timezoneObject = new ScriptObject();
                timezoneObject["Time"] = formattedTime;
                timezoneObject["Name"] = timeZoneOffsetString;
            }
            routeAgentObject["Timezone"] = timezoneObject;

            return routeAgentObject;
        }

        private ScriptObject CreateRouteAgentObject(ConversationSessionContextAgent contextAgent)
        {
            var routeAgentObject = new ScriptObject();

            // Add caller number context
            // TODO

            return routeAgentObject;
        }

        private async Task<ScriptObject> CreateRouteLanguageObject(ConversationSessionContextLanguage? contextLanguageData, string currentLanguageCode)
        {
            var languageContainerObject = new ScriptObject(); // Will contain CurrentLanguage and Multilangauges

            // Default empty objects to prevent template errors
            var currentLangObject = new ScriptObject { { "Code", "unknown" }, { "Name", "Unknown" } };
            var multiLangObject = new ScriptObject { { "Enabled", false }, { "Languages", new ScriptArray() } };

            if (contextLanguageData != null && !string.IsNullOrEmpty(currentLanguageCode))
            {
                // 1. Current Language
                var currentLanguageResult = await _languagesManager.GetLanguageByCode(currentLanguageCode);
                var currentLanguageName = currentLanguageResult.Success && currentLanguageResult.Data != null
                    ? currentLanguageResult.Data.Name
                    : currentLanguageCode;

                currentLangObject["Code"] = currentLanguageCode;
                currentLangObject["Name"] = currentLanguageName;

                // 2. Multi-languages Info
                bool multiLangEnabled = contextLanguageData.MultiLanguageEnabled != null &&
                                        contextLanguageData.MultiLanguageEnabled.Value &&
                                        contextLanguageData.EnabledMultiLanguages != null &&
                                        contextLanguageData.EnabledMultiLanguages.Count > 0;

                multiLangObject["Enabled"] = multiLangEnabled;

                if (multiLangEnabled)
                {
                    var languagesList = new ScriptArray();
                    foreach (var enabledLang in contextLanguageData.EnabledMultiLanguages!)
                    {
                        var langInfoResult = await _languagesManager.GetLanguageByCode(enabledLang.LanguageCode);
                        var langName = langInfoResult.Success && langInfoResult.Data != null
                            ? langInfoResult.Data.Name
                            : enabledLang.LanguageCode;

                        var langInfoObject = new ScriptObject();
                        langInfoObject.Add("Code", enabledLang.LanguageCode);
                        langInfoObject.Add("Name", langName);
                        languagesList.Add(langInfoObject);
                    }
                    multiLangObject["Languages"] = languagesList;
                }
            }
            else
            {
                if (contextLanguageData == null) _logger.LogError("BusinessAppRouteLanguage data was null when creating language object.");
                if (string.IsNullOrEmpty(currentLanguageCode)) _logger.LogError("Current language code was null or empty when creating language object.");
            }

            languageContainerObject.Add("CurrentLanguage", currentLangObject);
            languageContainerObject.Add("Multilangauges", multiLangObject);

            return languageContainerObject;
        }

        private (List<BusinessAppTool> tools, List<BusinessAppAgent> agents, List<BusinessAppAgentScript> scripts, bool hasDTMFRequestTool, bool hasSendSMSTool, bool hasRetrieveKnowledgeBaseTool) GetScriptNodesData(BusinessAppAgentScript currentScriptToCheck, BusinessApp businessApp, BusinessAppAgent sessionRouteAgent)
        {
            var (tools, agents, scripts, hasDTMFRequestTool, hasSendSMSTool, hasRetrieveKnowledgeBaseTool) = (new List<BusinessAppTool>(), new List<BusinessAppAgent>(), new List<BusinessAppAgentScript>(), false, false, false);

            foreach (var node in currentScriptToCheck.Nodes)
            {
                if (node.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool)
                {
                    var customToolNode = node as BusinessAppAgentScriptCustomToolNode;
                    if (customToolNode != null)
                    {
                        var alreadyAddedTool = tools.Find(t => t.Id == customToolNode.ToolId) != null;
                        if (!alreadyAddedTool)
                        {
                            var tool = businessApp.Tools.Find(t => t.Id == customToolNode.ToolId);
                            if (tool != null)
                            {
                                tools.Add(tool);
                            }
                        }
                    }
                }
                else if (node.NodeType == BusinessAppAgentScriptNodeTypeENUM.ExecuteSystemTool)
                {
                    var systemToolNode = node as BusinessAppAgentScriptSystemToolNode;

                    if (systemToolNode != null)
                    {
                        if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent)
                        {
                            var transferToAgentNode = node as BusinessAppAgentScriptTransferToAgentToolNode;
                            if (transferToAgentNode != null)
                            {
                                var alreadyAddedAgent = agents.Find(a => a.Id == transferToAgentNode.AgentId) != null;
                                if (!alreadyAddedAgent)
                                {
                                    var agent = businessApp.Agents.Find(a => a.Id == transferToAgentNode.AgentId);
                                    if (agent != null)
                                    {
                                        agents.Add(agent);
                                    }
                                }
                            }
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext)
                        {
                            var addScriptToContextNode = node as BusinessAppAgentScriptAddScriptToContextToolNode;
                            if (addScriptToContextNode != null)
                            {
                                var alreadyAddedScript = scripts.Find(s => s.Id == addScriptToContextNode.ScriptId) != null;
                                if (!alreadyAddedScript && addScriptToContextNode.ScriptId != currentScriptToCheck.Id)
                                {
                                    var scriptData = sessionRouteAgent.Scripts.Find(s => s.Id == addScriptToContextNode.ScriptId);
                                    if (scriptData != null)
                                    {
                                        scripts.Add(scriptData);
                                    }
                                }
                            }
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput)
                        {
                            hasDTMFRequestTool = true;
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS)
                        {
                            hasSendSMSTool = true;
                        }
                        else if (systemToolNode.ToolType == BusinessAppAgentScriptNodeSystemToolTypeENUM.RetrieveKnowledgeBase)
                        {
                            hasRetrieveKnowledgeBaseTool = true;
                        }
                    }
                }
            }

            return (tools, agents, scripts, hasDTMFRequestTool, hasSendSMSTool, hasRetrieveKnowledgeBaseTool);
        }

        private string GetLocalizedString(Dictionary<string, string> dictionary, string languageCode, string defaultValue)
        {
            if (dictionary == null) return defaultValue;

            // Try exact match
            if (dictionary.TryGetValue(languageCode, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;

            // Try language without region
            var baseLanguage = languageCode.Split('-')[0];
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(baseLanguage) && !string.IsNullOrWhiteSpace(dictionary[key]))
                    return dictionary[key];
            }

            // Try English as fallback
            if (dictionary.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue))
                return enValue;

            // Return any available value
            var firstNonEmpty = dictionary.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            return firstNonEmpty ?? defaultValue;
        }

        private List<string> GetLocalizedList(Dictionary<string, List<string>> dictionary, string languageCode)
        {
            if (dictionary == null) return new List<string>();

            // Try exact match
            if (dictionary.TryGetValue(languageCode, out var value) && value != null && value.Any())
                return value;

            // Try language without region
            var baseLanguage = languageCode.Split('-')[0];
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(baseLanguage) && dictionary[key] != null && dictionary[key].Any())
                    return dictionary[key];
            }

            // Try English as fallback
            if (dictionary.TryGetValue("en", out var enValue) && enValue != null && enValue.Any())
                return enValue;

            // Return first available list
            return dictionary.Values.FirstOrDefault(v => v != null && v.Any()) ?? new List<string>();
        }

        private Dictionary<string, string> GetLocalizedDictionary(Dictionary<string, Dictionary<string, string>> dictionary, string languageCode)
        {
            if (dictionary == null) return new Dictionary<string, string>();

            // Try exact match
            if (dictionary.TryGetValue(languageCode, out var value) && value != null)
                return value;

            // Try language without region
            var baseLanguage = languageCode.Split('-')[0];
            foreach (var key in dictionary.Keys)
            {
                if (key.StartsWith(baseLanguage) && dictionary[key] != null)
                    return dictionary[key];
            }

            // Try English as fallback
            if (dictionary.TryGetValue("en", out var enValue) && enValue != null)
                return enValue;

            // Return first available dictionary
            return dictionary.Values.FirstOrDefault(v => v != null) ?? new Dictionary<string, string>();
        }
    }
}
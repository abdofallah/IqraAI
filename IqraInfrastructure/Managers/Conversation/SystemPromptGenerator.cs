using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.LLM;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation
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

        public async Task<FunctionReturnResult<string?>> GenerateSystemPrompt(
            BusinessApp businessApp,
            BusinessAppAgent agent,
            BusinessAppRoute route,
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

                // Initialize Scriban template
                var template = Template.Parse(systemPromptForLanguage);
                
                if (template.HasErrors)
                {
                    result.Code = "GenerateSystemPrompt:4";
                    result.Message = "Error parsing system prompt template: " + string.Join(", ", template.Messages);
                    return result;
                }

                // Create template context
                var templateContext = new TemplateContext();
                var scriptObject = new ScriptObject();
                
                // Setup model with localized data
                var modelObject = new ScriptObject();
                
                // Add Agent data
                var agentObject = new ScriptObject();
                agentObject["Personality"] = CreateAgentPersonalityObject(agent.Personality, languageCode);
                agentObject["Context"] = CreateAgentContextObject(agent.Context);
                agentObject["Scripts"] = CreateAgentScriptsObject(new List<BusinessAppAgentScript>(), languageCode); // todo only use the opening script or enabled script
                agentObject["ScriptTools"] = CreateAgentScriptToolsObject(new List<BusinessAppTool>(), languageCode); // TODO get the tools used by the scripts
                modelObject["Agent"] = agentObject;
                
                // Add Context (company) data
                var contextObject = new ScriptObject();
                contextObject["Branding"] = CreateBrandingObject(businessApp.Context.Branding, languageCode);
                contextObject["Branches"] = CreateBranchesObject(businessApp.Context.Branches, languageCode);
                contextObject["Services"] = CreateServicesObject(businessApp.Context.Services, languageCode);
                contextObject["Products"] = CreateProductsObject(businessApp.Context.Products, languageCode);
                modelObject["Context"] = contextObject;

                // Add Route data
                var routeObject = new ScriptObject();
                routeObject["Agent"] = CreateRouteAgentObject(route.Agent);
                modelObject["Route"] = routeObject;

                // Add Session data
                // TODO
                var sessionObject = new ScriptObject();
                var callerObject = new ScriptObject();
                callerObject["PhoneNumber"] = "Unknown"; // Replace with actual caller number when available
                sessionObject["Caller"] = callerObject;
                modelObject["Session"] = sessionObject;

                // Register helper templates
                RegisterHelperTemplates(scriptObject);

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
                result.Code = "GenerateSystemPrompt:5";
                result.Message = "Error generating system prompt: " + ex.Message;
            }

            return result;
        }

        #region Template object creation methods

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

        private ScriptArray CreateAgentScriptsObject(List<BusinessAppAgentScript> scripts, string languageCode)
        {
            var scriptsArray = new ScriptArray();
            if (scripts != null)
            {
                foreach (var script in scripts)
                {
                    // Add script properties as needed
                    // For now we'll just add a placeholder
                    scriptsArray.Add($"TODO SCRIPT {GetLocalizedString(script.General.Name, languageCode, "")}");
                }
            }
            return scriptsArray;
        }

        private ScriptArray CreateAgentScriptToolsObject(List<BusinessAppTool> tools, string languageCode)
        {
            var toolsArray = new ScriptArray();
            if (tools != null)
            {
                foreach (var tool in tools)
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

                    toolsArray.Add(toolObject);
                }
            }
            return toolsArray;
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
                        workingHourObject["Name"] = workingHourDay.Key;
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
                    serviceObject["LongDescription"] = GetLocalizedString(service.LongDescription, languageCode, "");
                    serviceObject["AvailableAtBranches"] = service.AvailableAtBranches ?? new List<string>();
                    serviceObject["RelatedProducts"] = service.RelatedProducts ?? new List<string>();
                    
                    // Add other information
                    var otherInfo = GetLocalizedDictionary(service.OtherInformation, languageCode);
                    var infoObject = new ScriptObject();
                    foreach (var info in otherInfo)
                    {
                        infoObject[info.Key] = info.Value;
                    }
                    serviceObject["OtherInformation"] = infoObject;
                    
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
                    productObject["LongDescription"] = GetLocalizedString(product.LongDescription, languageCode, "");
                    productObject["AvailableAtBranches"] = product.AvailableAtBranches ?? new List<string>();
                    
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

        private ScriptObject CreateRouteAgentObject(BusinessAppRouteAgent routeAgent)
        {
            var routeAgentObject = new ScriptObject();
            
            // Add timezone information
            var timezoneObject = new ScriptObject();
            timezoneObject["Now"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            routeAgentObject["Timezone"] = timezoneObject;
            
            // Add caller number context
            routeAgentObject["CallerNumberInContext"] = routeAgent.CallerNumberInContext;
            
            return routeAgentObject;
        }

        private void RegisterHelperTemplates(ScriptObject scriptObject)
        {
            // Register the service template function
            scriptObject.Add("services_template", new Func<ScriptObject, string>(service => {
                var sb = new StringBuilder();
                sb.AppendLine($"<Service{service["Id"]}>");
                sb.AppendLine($"Name: {service["Name"]}");
                sb.AppendLine($"Short Description: {service["ShortDescription"]}");
                sb.AppendLine($"Long Description: {service["LongDescription"]}");
                
                if (service["AvailableAtBranches"] is ScriptArray branches && branches.Count > 0)
                {
                    sb.AppendLine("Available at branches:");
                    foreach (var branch in branches)
                    {
                        sb.AppendLine($"- {branch}");
                    }
                }
                
                if (service["RelatedProducts"] is ScriptArray products && products.Count > 0)
                {
                    sb.AppendLine("Related products:");
                    foreach (var product in products)
                    {
                        sb.AppendLine($"- {product}");
                    }
                }
                
                if (service["OtherInformation"] is ScriptObject otherInfo)
                {
                    sb.AppendLine("Additional information:");
                    foreach (var key in otherInfo.Keys)
                    {
                        sb.AppendLine($"- {key}: {otherInfo[key]}");
                    }
                }
                
                sb.AppendLine($"</Service{service["Id"]}>");
                return sb.ToString();
            }));
            
            // Register the product template function
            scriptObject.Add("products_template", new Func<ScriptObject, string>(product => {
                var sb = new StringBuilder();
                sb.AppendLine($"<Product{product["Id"]}>");
                sb.AppendLine($"Name: {product["Name"]}");
                sb.AppendLine($"Short Description: {product["ShortDescription"]}");
                sb.AppendLine($"Long Description: {product["LongDescription"]}");
                
                if (product["AvailableAtBranches"] is ScriptArray branches && branches.Count > 0)
                {
                    sb.AppendLine("Available at branches:");
                    foreach (var branch in branches)
                    {
                        sb.AppendLine($"- {branch}");
                    }
                }
                
                if (product["OtherInformation"] is ScriptObject otherInfo)
                {
                    sb.AppendLine("Additional information:");
                    foreach (var key in otherInfo.Keys)
                    {
                        sb.AppendLine($"- {key}: {otherInfo[key]}");
                    }
                }
                
                sb.AppendLine($"</Product{product["Id"]}>");
                return sb.ToString();
            }));
            
            // Register the agent scripts template function
            scriptObject.Add("agent_scripts", new Func<ScriptObject, string>(script => {
                return "TODO SCRIPT";
            }));
        }

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
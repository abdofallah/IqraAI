using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Agent;
using Microsoft.Extensions.Logging;
using System.Text;

namespace IqraInfrastructure.Managers.Conversation
{
    public class SystemPromptGenerator
    {
        private readonly ILogger<SystemPromptGenerator> _logger;

        public SystemPromptGenerator(ILogger<SystemPromptGenerator> logger)
        {
            _logger = logger;
        }

        public string GenerateSystemPrompt(
            BusinessApp businessApp,
            BusinessAppAgent agent,
            BusinessAppRoute route,
            string languageCode)
        {
            try
            {
                var sb = new StringBuilder();

                // 1. Add agent personality
                sb.AppendLine(GeneratePersonalitySection(agent, languageCode));

                // 2. Add business context information
                if (agent.Context.UseBranding)
                {
                    sb.AppendLine(GenerateBrandingSection(businessApp.Context.Branding, languageCode));
                }

                if (agent.Context.UseBranches)
                {
                    sb.AppendLine(GenerateBranchesSection(businessApp.Context.Branches, languageCode));
                }

                if (agent.Context.UseServices)
                {
                    sb.AppendLine(GenerateServicesSection(businessApp.Context.Services, languageCode));
                }

                if (agent.Context.UseProducts)
                {
                    sb.AppendLine(GenerateProductsSection(businessApp.Context.Products, languageCode));
                }

                // 3. Add conversation instructions
                sb.AppendLine(GenerateConversationInstructions(agent, route, languageCode));

                // 4. Add response format instructions
                sb.AppendLine(GenerateResponseFormatInstructions());

                _logger.LogInformation("Generated system prompt for language {LanguageCode}", languageCode);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating system prompt for language {LanguageCode}", languageCode);
                // Return a basic prompt as fallback
                return "You are a helpful AI assistant for a business. Be polite and provide accurate information.";
            }
        }

        private string GeneratePersonalitySection(BusinessAppAgent agent, string languageCode)
        {
            var sb = new StringBuilder();

            // Get language-specific personality data
            var name = GetLocalizedString(agent.Personality.Name, languageCode, "AI Assistant");
            var role = GetLocalizedString(agent.Personality.Role, languageCode, "Customer Service Representative");

            // Build the personality section
            sb.AppendLine($"# IDENTITY AND ROLE");
            sb.AppendLine($"You are {name}, a {role}.");
            sb.AppendLine();

            // Add capabilities if available
            var capabilities = GetLocalizedList(agent.Personality.Capabilities, languageCode);
            if (capabilities.Any())
            {
                sb.AppendLine("## Capabilities");
                foreach (var capability in capabilities)
                {
                    sb.AppendLine($"- {capability}");
                }
                sb.AppendLine();
            }

            // Add tone if available
            var tones = GetLocalizedList(agent.Personality.Tone, languageCode);
            if (tones.Any())
            {
                sb.AppendLine("## Communication Style");
                sb.AppendLine("You should communicate in the following way:");
                foreach (var tone in tones)
                {
                    sb.AppendLine($"- {tone}");
                }
                sb.AppendLine();
            }

            // Add ethics if available
            var ethics = GetLocalizedList(agent.Personality.Ethics, languageCode);
            if (ethics.Any())
            {
                sb.AppendLine("## Ethical Guidelines");
                sb.AppendLine("Follow these ethical guidelines:");
                foreach (var ethic in ethics)
                {
                    sb.AppendLine($"- {ethic}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateBrandingSection(BusinessAppContextBranding branding, string languageCode)
        {
            var sb = new StringBuilder();

            var name = GetLocalizedString(branding.Name, languageCode, "the company");
            var country = GetLocalizedString(branding.Country, languageCode, "");
            var email = GetLocalizedString(branding.Email, languageCode, "");
            var phone = GetLocalizedString(branding.Phone, languageCode, "");
            var website = GetLocalizedString(branding.Website, languageCode, "");

            sb.AppendLine("# COMPANY INFORMATION");
            sb.AppendLine($"You represent {name}.");

            if (!string.IsNullOrWhiteSpace(country))
                sb.AppendLine($"The company is based in {country}.");

            if (!string.IsNullOrWhiteSpace(email))
                sb.AppendLine($"Contact email: {email}");

            if (!string.IsNullOrWhiteSpace(phone))
                sb.AppendLine($"Contact phone: {phone}");

            if (!string.IsNullOrWhiteSpace(website))
                sb.AppendLine($"Website: {website}");

            // Add other information if available
            var otherInfo = GetLocalizedDictionary(branding.OtherInformation, languageCode);
            foreach (var info in otherInfo)
            {
                sb.AppendLine($"{info.Key}: {info.Value}");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private string GenerateBranchesSection(List<BusinessAppContextBranch> branches, string languageCode)
        {
            if (!branches.Any()) return "";

            var sb = new StringBuilder();
            sb.AppendLine("# BRANCH LOCATIONS");

            foreach (var branch in branches)
            {
                var name = GetLocalizedString(branch.General.Name, languageCode, "Branch");
                var address = GetLocalizedString(branch.General.Address, languageCode, "");
                var phone = GetLocalizedString(branch.General.Phone, languageCode, "");

                sb.AppendLine($"## {name}");
                if (!string.IsNullOrWhiteSpace(address))
                    sb.AppendLine($"Address: {address}");

                if (!string.IsNullOrWhiteSpace(phone))
                    sb.AppendLine($"Phone: {phone}");

                // Add working hours
                sb.AppendLine("Working Hours:");
                foreach (var hours in branch.WorkingHours)
                {
                    if (hours.Value.IsClosed)
                    {
                        sb.AppendLine($"- {hours.Key}: Closed");
                    }
                    else
                    {
                        var timeRanges = string.Join(", ", hours.Value.Timings.Select(t => $"{t.Item1} - {t.Item2}"));
                        sb.AppendLine($"- {hours.Key}: {timeRanges}");
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateServicesSection(List<BusinessAppContextService> services, string languageCode)
        {
            if (!services.Any()) return "";

            var sb = new StringBuilder();
            sb.AppendLine("# SERVICES");

            foreach (var service in services)
            {
                var name = GetLocalizedString(service.Name, languageCode, "Service");
                var description = GetLocalizedString(service.LongDescription, languageCode, "");

                sb.AppendLine($"## {name}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine(description);

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateProductsSection(List<BusinessAppContextProduct> products, string languageCode)
        {
            if (!products.Any()) return "";

            var sb = new StringBuilder();
            sb.AppendLine("# PRODUCTS");

            foreach (var product in products)
            {
                var name = GetLocalizedString(product.Name, languageCode, "Product");
                var description = GetLocalizedString(product.LongDescription, languageCode, "");

                sb.AppendLine($"## {name}");
                if (!string.IsNullOrWhiteSpace(description))
                    sb.AppendLine(description);

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string GenerateConversationInstructions(BusinessAppAgent agent, BusinessAppRoute route, string languageCode)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# CONVERSATION GUIDELINES");

            // Add greeting message if available
            var greeting = GetLocalizedString(agent.Utterances.GreetingMessage, languageCode, "");
            if (!string.IsNullOrWhiteSpace(greeting))
            {
                sb.AppendLine("## Initial Greeting");
                sb.AppendLine($"Begin the conversation with: \"{greeting}\"");
                sb.AppendLine();
            }

            // Add conversation type instructions
            sb.AppendLine("## Conversation Style");
            switch (route.Agent.ConversationType)
            {
                case AgentConversationTypeENUM.Interruptible:
                    int words = route.Agent.InterruptibleConversationTypeWords ?? 3;
                    sb.AppendLine($"Keep your responses concise, ideally {words} sentences or less at a time.");
                    sb.AppendLine("Pause naturally between points to allow the caller to interject if needed.");
                    break;
                case AgentConversationTypeENUM.TurnByTurn:
                    sb.AppendLine("Provide complete answers in a single response.");
                    sb.AppendLine("Make sure you cover all relevant information before stopping.");
                    break;
                default:
                    sb.AppendLine("Maintain a natural conversational flow.");
                    break;
            }
            sb.AppendLine();

            // Add caller information instructions
            sb.AppendLine("## Caller Information");
            if (route.Agent.CallerNumberInContext)
            {
                sb.AppendLine("You will have access to the caller's phone number for identification purposes.");
            }

            sb.AppendLine();

            return sb.ToString();
        }

        private string GenerateResponseFormatInstructions()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# RESPONSE FORMAT");
            sb.AppendLine("You must prefix all your responses with one of these formats:");
            sb.AppendLine();
            sb.AppendLine("1. For normal responses to the customer:");
            sb.AppendLine("response_to_customer: Your message to the customer");
            sb.AppendLine();
            sb.AppendLine("2. To execute a tool or perform an action:");
            sb.AppendLine("execute_tool: toolName:param1=value1,param2=value2");
            sb.AppendLine();
            sb.AppendLine("3. To end the call:");
            sb.AppendLine("end_call: Reason for ending the call");
            sb.AppendLine();
            sb.AppendLine("Always use the appropriate prefix for your responses.");

            return sb.ToString();
        }

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
namespace IqraInfrastructure.Helpers
{
    public enum VariableType { String, Number, Boolean, Datetime, Object, Function }

    public class CustomVariableInputTemplateVariableDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public VariableType Type { get; set; }
        public List<CustomVariableInputTemplateVariableDefinition> Schema { get; set; } = new List<CustomVariableInputTemplateVariableDefinition>();
        public List<CustomVariableInputTemplateArgumentDefinition> Args { get; set; } = new List<CustomVariableInputTemplateArgumentDefinition>();
    }

    public class CustomVariableInputTemplateArgumentDefinition
    {
        public string Name { get; set; }
        public bool IsLiteral { get; set; }
    }

    public class CustomVariableInputTemplateValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public static class CustomVariableInputTemplateValidator
    {
        private const string Opener = "{={";
        private const string Closer = "}=}";

        public static CustomVariableInputTemplateValidationResult Validate(string templateText, List<CustomVariableInputTemplateVariableDefinition> allowedDefinitions)
        {
            var result = new CustomVariableInputTemplateValidationResult();
            if (string.IsNullOrEmpty(templateText))
            {
                return result; // An empty template is valid.
            }

            var tokens = ExtractTokens(templateText);

            foreach (var token in tokens)
            {
                // Check if it's a function call by looking for parentheses
                int parenIndex = token.IndexOf('(');
                if (parenIndex > -1)
                {
                    // It's a function
                    string functionId = token.Substring(0, parenIndex).Trim();
                    var definition = allowedDefinitions.FirstOrDefault(d => d.Id == functionId && d.Type == VariableType.Function);

                    if (definition == null)
                    {
                        result.Errors.Add($"The function '{functionId}' is not an allowed function.");
                    }
                    else
                    {
                        // Placeholder for future, more complex argument validation
                        // For example: parse the arguments and check their types and counts.
                        // ValidateFunctionArguments(token, definition, result);
                    }
                }
                else
                {
                    // It's a variable or object path
                    string variablePath = token.Trim();
                    if (!IsValidVariablePath(variablePath, allowedDefinitions))
                    {
                        result.Errors.Add($"The variable '{variablePath}' is not a valid or allowed variable.");
                    }
                }
            }

            return result;
        }

        private static List<string> ExtractTokens(string template)
        {
            var tokens = new List<string>();
            int currentIndex = 0;

            while (currentIndex < template.Length)
            {
                int openerIndex = template.IndexOf(Opener, currentIndex);
                if (openerIndex == -1)
                {
                    break; // No more openers found
                }

                int searchIndex = openerIndex + Opener.Length;
                int nestingLevel = 1;

                while (searchIndex < template.Length)
                {
                    int nextOpener = template.IndexOf(Opener, searchIndex);
                    int nextCloser = template.IndexOf(Closer, searchIndex);

                    if (nextCloser == -1)
                    {
                        // Unmatched opener, break the loop to prevent errors
                        searchIndex = template.Length;
                        break;
                    }

                    if (nextOpener != -1 && nextOpener < nextCloser)
                    {
                        // Found a nested opener before the next closer
                        nestingLevel++;
                        searchIndex = nextOpener + Opener.Length;
                    }
                    else
                    {
                        // Found a closer
                        nestingLevel--;
                        if (nestingLevel == 0)
                        {
                            // Found the matching closer for our top-level token
                            string token = template.Substring(openerIndex + Opener.Length, nextCloser - (openerIndex + Opener.Length));
                            tokens.Add(token);
                            currentIndex = nextCloser + Closer.Length;
                            break; // Exit the search loop for this token
                        }
                        searchIndex = nextCloser + Closer.Length;
                    }
                }
                if (nestingLevel != 0)
                {
                    // This means the outer while loop will eventually end because openerIndex was not found,
                    // but we might want to log this as a template syntax error.
                    // For now, we just move past the opener to avoid an infinite loop.
                    currentIndex = openerIndex + Opener.Length;
                }
            }
            return tokens;
        }

        private static bool IsValidVariablePath(string path, List<CustomVariableInputTemplateVariableDefinition> schema)
        {
            var parts = path.Split('.');
            var currentLevelSchema = schema;

            foreach (var part in parts)
            {
                var foundDefinition = currentLevelSchema.FirstOrDefault(def => def.Id == part);
                if (foundDefinition == null)
                {
                    return false; // A part of the path was not found
                }

                // Move to the next level for the next iteration
                currentLevelSchema = foundDefinition.Schema;
            }

            return true; // The entire path was successfully traversed
        }
    }
}

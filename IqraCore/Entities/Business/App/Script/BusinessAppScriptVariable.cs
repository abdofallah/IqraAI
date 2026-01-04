using IqraCore.Attributes;

namespace IqraCore.Entities.Business.App.Script
{
    public class BusinessAppScriptVariable
    {
        /// <summary>
        /// The identifier used in Scriban templates (e.g., {{ variables.my_key }}).
        /// Must be Alphanumeric + Underscores.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        public BusinessAppScriptVariableTypeENUM Type { get; set; } = BusinessAppScriptVariableTypeENUM.String;

        /// <summary>
        /// The initial value. Stored as string, converted at runtime.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// If true, this variable's value is injected into the LLM System Prompt context.
        /// If false, it is hidden from the LLM (useful for sensitive data like PINs).
        /// </summary>
        public bool IsVisibleToAgent { get; set; } = true;

        /// <summary>
        /// If true, the AI Agent is allowed to modify this variable (e.g. via extraction or tool output).
        /// If false, it is Read-Only (Static/Constant).
        /// </summary>
        public bool IsEditableByAI { get; set; } = false;

        [MultiLanguageProperty]
        public Dictionary<string, string> Description { get; set; } = new();
    }

    public enum BusinessAppScriptVariableTypeENUM
    {
        String = 1,
        Number = 2,
        Boolean = 3
    }
}

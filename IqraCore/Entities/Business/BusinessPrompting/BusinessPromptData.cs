namespace IqraCore.Entities.Business
{
    public class BusinessPromptData
    {
        public List<string> LanguagesEnabled { get; set; }

        public Dictionary<string, Dictionary<string, string>> TemplateVariables { get; set; }

        public Dictionary<string, string> BusinessInitialMessage { get; set; }
        public Dictionary<string, string> BusinessGeneralData { get; set; }
        public Dictionary<string, string> BusinessAboutAndFeatures { get; set; }
        public Dictionary<string, string> BusinessTeamMembers { get; set; }
        public Dictionary<string, string> BusinessServices { get; set; }
        public BusinessWorkingHours BusinessWorkingHours { get; set; }

        public List<BusinessFunctionTool> BusinessFunctionTools { get; set; }
    }
}

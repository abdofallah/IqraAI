namespace IqraCore.Entities.Business
{
    public class Business
    {
        public long BusinessId { get; set; }

        public List<string> LanguagesEnabled { get; set; }

        public Dictionary<string, string> BusinessName { get; set; }

        public string BusinessPhoneNumber { get; set; }

        public Dictionary<string, string> BusinessSystemPrompt { get; set; }
        public Dictionary<string, string> BusinessInitialMessage { get; set; }


        public BusinessAzureSettings? BusinessAzureSettings { get; set; }
    }
}

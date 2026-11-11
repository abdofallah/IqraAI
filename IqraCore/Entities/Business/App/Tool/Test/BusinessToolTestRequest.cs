namespace IqraCore.Entities.Business
{
    public class BusinessToolTestRequest
    {
        public Dictionary<string, object?> Parameters { get; set; } = new();
        public string LanguageCode { get; set; } = "en";
    }
}

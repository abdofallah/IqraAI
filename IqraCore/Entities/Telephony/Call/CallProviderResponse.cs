namespace IqraCore.Entities.Telephony.Call
{
    public class CallProviderResponse
    {
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public Dictionary<string, string> ProviderMetadata { get; set; } = new Dictionary<string, string>();
    }
}

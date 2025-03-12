namespace IqraCore.Models.Server
{
    public class BackendInitiateCallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string CallId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

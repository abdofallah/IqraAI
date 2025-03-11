namespace IqraCore.Models.Server
{
    public class ServerSelectionResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string ServerEndpoint { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
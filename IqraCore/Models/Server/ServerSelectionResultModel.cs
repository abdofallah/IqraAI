namespace IqraCore.Models.Server
{
    public class ServerSelectionResultModel
    {
        public string ServerId { get; set; } = string.Empty;
        public string ServerEndpoint { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
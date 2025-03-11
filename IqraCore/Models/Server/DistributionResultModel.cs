namespace IqraCore.Models.Server
{
    public class DistributionResultModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string QueueId { get; set; } = string.Empty;
        public string MediaUrl { get; set; } = string.Empty;
        public string BackendServerId { get; set; } = string.Empty;
    }
}
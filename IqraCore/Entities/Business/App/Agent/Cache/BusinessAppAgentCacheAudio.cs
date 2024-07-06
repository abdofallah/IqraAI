namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCacheAudio
    {
        public string Query { get; set; } = string.Empty;
        public int UnusedExpiryHours { get; set; } = 24;
        public string AudioUrl { get; set; } = string.Empty;
    }
}

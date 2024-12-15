namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheAudio
    {
        public string Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public int UnusedExpiryHours { get; set; } = 24;
    }
}

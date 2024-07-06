namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCacheSettings
    {
        public bool AutoCacheAudioResponses { get; set; } = false;
        public int? AutoCacheAudioResponsesDefaultExpiryHours { get; set; } = 24;
    }
}

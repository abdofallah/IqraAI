namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentAutoCacheAudioSettings
    {
        public bool AutoCacheAudioResponses { get; set; } = false;
        public int? AutoCacheAudioResponsesDefaultExpiryHours { get; set; } = 24;
        public string? AutoCacheAudioResponseCacheGroupId { get; set; } = null;
    }
}

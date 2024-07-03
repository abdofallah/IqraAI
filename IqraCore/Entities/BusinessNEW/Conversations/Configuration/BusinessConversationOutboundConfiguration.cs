namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationOutboundConfiguration : BusinessConversationConfiguration
    {
        public bool? RetryOnDecline { get; set; }
        public int? RetryOnDeclineCount { get; set; }
        public int? RetryOnDeclineDelayMS { get; set; }

        public bool? RetryOnMisscall { get; set; }
        public int? RetryOnMisscallCount { get; set; }
        public int? RetryOnMisscallDelayMS { get; set; }
    }
}

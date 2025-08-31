namespace IqraCore.Entities.Configuration
{
    public class BusinessManagerInitalizationSettings
    {
        public bool InitalizeSettingsManager { get; set; } = false;
        public bool InitalizeToolsManager { get; set; } = false;
        public bool InitalizeContextManager { get; set; } = false;
        public bool InitalizeCacheManager { get; set; } = false;
        public bool InitalizeIntegrationsManager { get; set; } = false;
        public bool InitalizeAgentsManager { get; set; } = false;
        public bool InitalizeNumberManager { get; set; } = false;
        public bool InitalizeRoutesManager { get; set; } = false;
        public bool InitalizeConversationsManager { get; set; } = false;
        public bool InitalizeMakeCallManager { get; set; } = false;
        public bool InitalizeKnowledgeBaseManager { get; set; } = false;
        public bool InitalizeCampaignManager { get; set; } = false;
    }
}

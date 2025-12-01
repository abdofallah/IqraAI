namespace IqraCore.Entities.App.Configuration
{
    public static class S3StorageConfig
    {
        public const string IntegrationsLogoRepositoryBucketName = "iqra.integrations.logo";

        public const string BusinessLogoRepositoryBucketName = "iqra.business.logo";
        public const string BusinessToolAudioRepositoryBucketName = "iqra.business.tool.audio";
        public const string BusinessAgentAudioRepositoryBucketName = "iqra.business.agent.audio";     
        public const string BusinessConversationAudioRepositoryBucketName = "iqra.business.conversation.audio";

        public const string BusinessTTSAudioCacheStorageRepositoryBucketName = "iqra.business.tts.audio.cache";
    }
}
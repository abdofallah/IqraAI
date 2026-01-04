namespace IqraCore.Entities.Billing
{
    public static class BillingFeatureKey
    {
        // Call-related features
        public const string CallMinutes = "Call_Minutes";
        public const string CallConcurrency = "Call_Concurrency";
        public const string VoicemailDetection = "Call_VoicemailDetection";

        // TTS Cache features
        public const string TtsCacheStorageGb = "Tts_Cache_Storage_Gb";
        public const string TtsCacheConcurrency = "Tts_Cache_Concurrency";
        public const string TtsRetrivalMb = "Tts_Retrieval_Mb";

        // Knowledge Base features
        public const string KbStorageVectors1k = "Kb_Storage_Vectors_1k";
        public const string KbQueryConcurrency = "Kb_Query_Concurrency";

        // General Media Storage features
        public const string MediaStorageGb = "Media_Storage_Gb";
        public const string MediaRetrievalGb = "Media_Retrieval_Gb";
    }
}

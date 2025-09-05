namespace IqraCore.Entities.Business
{
    public class BusinessCachePermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public BusinessCacheMessageGroupPermission MessageGroup { get; set; } = new BusinessCacheMessageGroupPermission();
        public BusinessCacheAudioGroupPermission AudioGroup { get; set; } = new BusinessCacheAudioGroupPermission();
        public BusinessCacheEmbeddingGroupPermission EmbeddingGroup { get; set; } = new BusinessCacheEmbeddingGroupPermission();
    }

    public class BusinessCacheMessageGroupPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessCacheAudioGroupPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }

    public class BusinessCacheEmbeddingGroupPermission
    {
        public DateTime? DisabledFullAt { get; set; } = null;
        public string? DisabledFullReason { get; set; } = null;

        public DateTime? DisabledAddingAt { get; set; } = null;
        public string? DisabledAddingReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;

        public DateTime? DisabledDeletingAt { get; set; } = null;
        public string? DisabledDeletingReason { get; set; } = null;
    }
}

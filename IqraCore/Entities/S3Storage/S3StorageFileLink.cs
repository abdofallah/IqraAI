using IqraCore.Attributes;

namespace IqraCore.Entities.S3Storage
{
    [ExcludeInAllEndpoints]
    public class S3StorageFileLink
    {
        public string ObjectName { get; set; } = null!;

        public bool IsDefaultS3 { get; set; } = true;
        public string? OriginRegion { get; set; } = null;
    }
}

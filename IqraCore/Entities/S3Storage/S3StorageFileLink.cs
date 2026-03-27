namespace IqraCore.Entities.S3Storage
{
    public class S3StorageFileLink
    {
        public string ObjectName { get; set; } = null!;

        public bool IsDefaultS3 { get; set; } = true;
        public string? OriginRegion { get; set; } = null;
    }
}

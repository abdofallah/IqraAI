namespace IqraCore.Models.S3Storage
{
    public class S3SignedFileModel
    {
        public S3SignedFileModel() { }
        public S3SignedFileModel(string url, DateTime expiresAt)
        {
            Url = url;
            ExpiresAt = expiresAt;
        }

        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

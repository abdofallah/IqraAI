using IqraCore.Entities.S3Storage;

namespace IqraCore.Entities.WhiteLabel
{
    public class WhiteLabelBrandingData
    {
        public string PlatformName { get; set; } = "Iqra AI";
        public S3StorageFileLink? PlatformLogoS3StorageLink { get; set; } = null;
        public S3StorageFileLink? PlatformIconS3StorageLink { get; set; } = null;
        public string PlatformCustomCSS { get; set; } = string.Empty;
    }
}

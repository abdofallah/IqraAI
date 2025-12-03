using IqraCore.Entities.S3Storage;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentSettings
    {
        public S3StorageFileLink? BackgroundAudioS3StorageLink { get; set; } = null;
        public int? BackgroundAudioVolume { get; set; } = null;
    }
}

using IqraCore.Entities.S3Storage;

namespace IqraCore.Entities.Business
{
    public class BusinessAppToolAudio
    {
        public S3StorageFileLink? DuringExecutionAudioS3StorageLink { get; set; } = null;
        public int? DuringExecutionAudioVolume { get; set; } = null;

        public S3StorageFileLink? AfterExecutionAudioS3StorageLink { get; set; } = null;
        public int? AfterExecutionAudioVolume { get; set; } = null;
    }
}

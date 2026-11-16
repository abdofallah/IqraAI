using IqraCore.Entities.Business;
using IqraCore.Models.S3Storage;

namespace IqraCore.Models.User.Business.App.Tool
{
    public class BusinessAppToolAudioModel
    {
        public BusinessAppToolAudioModel(BusinessAppToolAudio data)
        {
            DuringExecutionAudioVolume = data.DuringExecutionAudioVolume;
            AfterExecutionAudioVolume = data.AfterExecutionAudioVolume;
        }

        public S3SignedFileModel? DuringExecutionAudioUrl { get; set; } = null;
        public int? DuringExecutionAudioVolume { get; set; } = null;

        public S3SignedFileModel? AfterExecutionAudioUrl { get; set; } = null;
        public int? AfterExecutionAudioVolume { get; set; } = null;
    }
}

namespace IqraCore.Entities.Business
{
    public class BusinessAppToolAudio
    {
        public string? DuringExecutionAudioUrl { get; set; } = null;
        public int? DuringExecutionAudioVolume { get; set; } = null;

        public string? AfterExecutionAudioUrl { get; set; } = null;
        public int? AfterExecutionAudioVolume { get; set; } = null;
    }
}

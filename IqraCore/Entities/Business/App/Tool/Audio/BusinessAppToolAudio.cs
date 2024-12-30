namespace IqraCore.Entities.Business
{
    public class BusinessAppToolAudio
    {
        public string? BeforeSpeaking { get; set; } = null;
        public int? BeforeSpeakingVolume { get; set; } = null;

        public string? DuringSpeaking { get; set; } = null;
        public int? DuringSpeakingVolume { get; set; } = null;

        public string? AfterSpeaking { get; set; } = null;
        public int? AfterSpeakingVolume { get; set; } = null;

    }
}

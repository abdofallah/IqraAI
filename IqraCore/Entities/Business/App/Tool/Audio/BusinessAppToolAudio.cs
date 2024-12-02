namespace IqraCore.Entities.Business
{
    public class BusinessAppToolAudio
    {
        public ToolAudioData BeforeSpeaking { get; set; } = new ToolAudioData();
        public ToolAudioData DuringSpeaking { get; set; } = new ToolAudioData();
        public ToolAudioData AfterSpeaking { get; set; } = new ToolAudioData();
    }

    public class ToolAudioData
    {
        public string? Name { get; set; }
        public string? URL { get; set; }
    }
}

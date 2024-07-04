using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessAppTool
    {
        public long Id { get; set; }
        public BusinessAppToolGeneral General { get; set; }
        public BusinessAppToolConfiguration Configuration { get; set; }
        public Dictionary<HttpStatusEnum, BusinessAppToolResponse> Response { get; set; }
        public BusinessAppToolAudio Audio { get; set; }
    }

    public class BusinessAppToolGeneral
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> ShortDescription { get; set; }
    }

    public class BusinessAppToolAudio
    {
        public string? BeforeSpeaking { get; set; }
        public string? DuringSpeaking { get; set; }
        public string? AfterSpeaking { get; set; }
    }
}

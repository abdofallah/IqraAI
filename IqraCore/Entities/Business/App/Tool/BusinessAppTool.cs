using IqraCore.Attributes;
using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessAppTool
    {
        public long Id { get; set; }
        public BusinessAppToolGeneral General { get; set; } = new BusinessAppToolGeneral();
        public BusinessAppToolConfiguration Configuration { get; set; } = new BusinessAppToolConfiguration();
        public Dictionary<HttpStatusEnum, BusinessAppToolResponse> Response { get; set; } = new Dictionary<HttpStatusEnum, BusinessAppToolResponse>();
        public BusinessAppToolAudio Audio { get; set; } = new BusinessAppToolAudio();
    }

    public class BusinessAppToolGeneral
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();
    }

    public class BusinessAppToolAudio
    {
        public string? BeforeSpeaking { get; set; } = null;
        public string? DuringSpeaking { get; set; } = null;
        public string? AfterSpeaking { get; set; } = null;
    }
}

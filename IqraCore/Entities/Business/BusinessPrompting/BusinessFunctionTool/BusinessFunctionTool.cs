using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessFunctionTool
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string SpeechOnExecution { get; set; }
        public string URL { get; set; }
        public HttpMethodEnum Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public BusinessFunctionToolInputSchemea InputSchemea { get; set; }
        public Dictionary<string, string> Response { get; set; }
    }
}

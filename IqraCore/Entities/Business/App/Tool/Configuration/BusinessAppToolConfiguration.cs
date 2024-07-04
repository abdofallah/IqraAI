using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessAppToolConfiguration
    {
        public List<BusinessAppToolConfigurationInputSchemea> InputSchemea { get; set; }
        public HttpMethodEnum RequestType { get; set; }
        public string Endpoint { get; set; }
        public Dictionary<string, string> ServiceName { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public HttpBodyEnum BodyType { get; set; }
        public object? BodyData { get; set; }
    }
}

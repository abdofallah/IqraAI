using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Business
{
    public class BusinessAppToolConfiguration
    {
        public List<BusinessAppToolConfigurationInputSchemea> InputSchemea { get; set; } = new List<BusinessAppToolConfigurationInputSchemea>();
        public HttpMethodEnum RequestType { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public HttpBodyEnum BodyType { get; set; } = HttpBodyEnum.None;
        public object? BodyData { get; set; } = null;
    }
}

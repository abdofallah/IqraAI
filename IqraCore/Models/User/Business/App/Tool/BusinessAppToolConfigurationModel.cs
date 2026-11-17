using IqraCore.Attributes;
using IqraCore.Entities.Business;
using IqraCore.Entities.Helper;

namespace IqraCore.Models.User.Business.App.Tool
{
    public class BusinessAppToolConfigurationModel
    {
        public BusinessAppToolConfigurationModel(BusinessAppToolConfiguration data)
        {
            InputSchemea = data.InputSchemea;
            RequestType = data.RequestType;
            Endpoint = data.Endpoint;
            Headers = data.Headers.ToDictionary(
                header => header.Key,
                header => new BusinessAppToolConfigurationHeaderModel
                {
                    Value = header.Value,
                    IsEncrypted = data.EncryptedHeaders.ContainsKey(header.Key)
                }
            );
            BodyType = data.BodyType;
            BodyData = data.BodyData;
        }

        public List<BusinessAppToolConfigurationInputSchemea> InputSchemea { get; set; }
        public HttpMethodEnum RequestType { get; set; }
        public string Endpoint { get; set; }

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, BusinessAppToolConfigurationHeaderModel> Headers { get; set; }
        public HttpBodyEnum BodyType { get; set; }
        public object? BodyData { get; set; }
    }
}

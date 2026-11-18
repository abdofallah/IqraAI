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
                header =>
                {
                    bool isEncrypted = data.EncryptedHeaders.ContainsKey(header.Key);
                    return new BusinessAppToolConfigurationHeaderModel
                    {
                        Value = isEncrypted ? null : header.Value,
                        IsEncrypted = isEncrypted
                    };
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

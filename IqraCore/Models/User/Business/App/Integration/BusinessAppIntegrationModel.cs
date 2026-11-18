using IqraCore.Attributes;
using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Integration
{
    public class BusinessAppIntegrationModel
    {
        public BusinessAppIntegrationModel(BusinessAppIntegration data)
        {
            Id = data.Id;
            Type = data.Type;
            FriendlyName = data.FriendlyName;
            Fields = data.Fields.ToDictionary(field => field.Key, field => (string?)field.Value);
            foreach (var encryptedField in data.EncryptedFields)
            {
                Fields[encryptedField.Key] = null;
            }
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public string FriendlyName { get; set; }

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, string?> Fields { get; set; }
    }
}

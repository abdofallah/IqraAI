using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrationData
    {
        public string Id { get; set; } = string.Empty;

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, object> FieldValues { get; set; } = new Dictionary<string, object>();
    }
}

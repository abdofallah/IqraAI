using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppIntegration
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        [ExcludeInAllEndpoints]
        public Dictionary<string, string> EncryptedFields { get; set; } = new Dictionary<string, string>();
    }
}

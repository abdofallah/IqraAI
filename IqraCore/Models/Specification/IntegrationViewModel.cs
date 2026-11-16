using IqraCore.Entities.Integrations;
using IqraCore.Models.S3Storage;

namespace IqraCore.Models.Specification
{
    public class IntegrationViewModel
    {
        public IntegrationViewModel(IntegrationData data)
        {
            Id = data.Id;
            Name = data.Name;
            Description = data.Description;
            DisabledAt = data.DisabledAt;
            //LogoUrl needs to be filled manually by generating presigned url
            Type = data.Type;
            Fields = data.Fields;
            Help = data.Help;
            IsOAuth = data.IsOAuth;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? DisabledAt { get; set; }
        public S3SignedFileModel? LogoUrl { get; set; }
        public List<string> Type { get; set; }
        public List<IntegrationFieldData> Fields { get; set; }
        public IntegrationHelpData Help { get; set; }
        public bool IsOAuth { get; set; }
    }
}

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
            Fields = data.Fields;
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public string FriendlyName { get; set; }
        public Dictionary<string, string> Fields { get; set; }
    }
}

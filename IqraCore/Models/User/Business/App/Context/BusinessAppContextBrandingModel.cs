using IqraCore.Entities.Business;

namespace IqraCore.Models.User.Business.App.Context
{
    public class BusinessAppContextBrandingModel
    {
        public BusinessAppContextBrandingModel(BusinessAppContextBranding data)
        {
            Name = data.Name;
            Country = data.Country;
            Email = data.Email;
            Phone = data.Phone;
            Website = data.Website;
            OtherInformation = data.OtherInformation;
        }

        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Country { get; set; }
        public Dictionary<string, string> Email { get; set; }
        public Dictionary<string, string> Phone { get; set; }
        public Dictionary<string, string> Website { get; set; }
        public Dictionary<string, Dictionary<string, string>> OtherInformation { get; set; }
    }
}

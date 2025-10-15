using IqraCore.Entities.User.WhiteLabel;
using IqraCore.Models.User.WhiteLabel.Plan;

namespace IqraCore.Models.User.WhiteLabel
{
    public class UserWhiteLabelDataModel
    {
        public UserWhiteLabelDataModel () { }
        public UserWhiteLabelDataModel (UserWhiteLabelData data)
        {
            IsActive = data.IsActive;
            DefaultBranding = new UserWhiteLabelBrandingDataModel(data.DefaultBranding);
            Domains = data.Domains.Select(x => new UserWhiteLabelDomainDataModel(x)).ToList();
            Plans = data.Plans.Select(x => new UserWhiteLabelPlanDataModel(x)).ToList();
        }

        public bool IsActive { get; set; } = false;
        public UserWhiteLabelBrandingDataModel DefaultBranding { get; set; } = new UserWhiteLabelBrandingDataModel();
        public List<UserWhiteLabelDomainDataModel> Domains { get; set; } = new List<UserWhiteLabelDomainDataModel>();
        public List<UserWhiteLabelPlanDataModel> Plans { get; set; } = new List<UserWhiteLabelPlanDataModel>();
    }
}

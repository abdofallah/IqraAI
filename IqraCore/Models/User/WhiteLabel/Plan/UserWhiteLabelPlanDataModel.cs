using IqraCore.Entities.User.WhiteLabel.Plan;
using IqraCore.Entities.User.WhiteLabel.Plan.Enum;
using IqraCore.Entities.User.WhiteLabel.Plan.Permission;

namespace IqraCore.Models.User.WhiteLabel.Plan
{
    public class UserWhiteLabelPlanDataModel
    {
        public UserWhiteLabelPlanDataModel() { }
        public UserWhiteLabelPlanDataModel(UserWhiteLabelPlanData data)
        {
            Id = data.Id;
            Permissions = data.Permissions;
            Name = data.Name;
            Description = data.Description;
            PricingModel = data.PricingModel;
            FixedMonthlyPrice = data.FixedMonthlyPrice;
            Features = data.Features.Select(x => new UserWhiteLabelPlanFeatureDataModel(x)).ToList();
            CreatedAt = data.CreatedAt;
            UpdatedAt = data.UpdatedAt;
            IsArchived = data.IsArchived;
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public UserWhiteLabelPlanPermissionData Permissions { get; set; } = new UserWhiteLabelPlanPermissionData();

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public UserWhiteLabelPlanPricingModelEnum PricingModel { get; set; } = UserWhiteLabelPlanPricingModelEnum.StandardPayAsYouGo;

        // Fixed Plan
        public decimal? FixedMonthlyPrice { get; set; } = null;

        public List<UserWhiteLabelPlanFeatureDataModel> Features { get; set; } = new List<UserWhiteLabelPlanFeatureDataModel>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; } = false;
    }
}

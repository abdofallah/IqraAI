using IqraCore.Entities.User.WhiteLabel.Plan;

namespace IqraCore.Models.User.WhiteLabel.Plan
{
    public class UserWhiteLabelPlanFeatureDataModel
    {
        public UserWhiteLabelPlanFeatureDataModel() { }
        public UserWhiteLabelPlanFeatureDataModel(UserWhiteLabelPlanFeatureData data)
        {
            Key = data.Key;
            DisplayName = data.DisplayName;
            Unit = data.Unit;
            UnitPlural = data.UnitPlural;
            IncludedLimit = data.IncludedLimit;
            UnitPrice = data.UnitPrice;
            OveragePrice = data.OveragePrice;
            IsRecurringMonthlyCharge = data.IsRecurringMonthlyCharge;
            VolumeDiscountTiers = data.VolumeDiscountTiers.Select(x => new UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel(x)).ToList();
        }

        public string Key { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;
        public string UnitPlural { get; set; } = string.Empty;

        public decimal IncludedLimit { get; set; } = 0;

        public decimal UnitPrice { get; set; } = 0;

        public decimal OveragePrice { get; set; } = 0;

        public bool IsRecurringMonthlyCharge { get; set; } = false;

        public List<UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel> VolumeDiscountTiers { get; set; } = new List<UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel>();
    }

    public class UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel
    {
        public UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel() { }
        public UserWhiteLabelPlanFeatureVolumeDiscountTierDataModel(UserWhiteLabelPlanFeatureVolumeDiscountTierData data)
        {
            MinimumMonthlyThreshold = data.MinimumMonthlyThreshold;
            DiscountPercentage = data.DiscountPercentage;
        }

        public int MinimumMonthlyThreshold { get; set; } = 0;

        public decimal DiscountPercentage { get; set; } = 0;
    }
}

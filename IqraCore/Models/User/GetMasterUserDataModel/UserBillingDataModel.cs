using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataModel
    {
        public UserBillingDataModel() { }
        public UserBillingDataModel (UserBillingData userBillingData)
        {
            CreditBalance = userBillingData.CreditBalance;

            Subscription = new UserBillingDataSubscriptionDetailsModel(userBillingData.Subscription);

            ActiveFeatureAddons = userBillingData.ActiveFeatureAddons
                .Select(addon => new UserBillingDataFeatureAddonModel(addon))
                .ToList();

            CurrentCycleUsage = new UserBillingDataCurrentBillingCycleUsageModel(userBillingData.CurrentCycleUsage);

            AutoRefill = new UserBillingDataAutoRefillSettingsModel(userBillingData.AutoRefill);
        }

        public decimal CreditBalance { get; set; } = 0.00m;

        public UserBillingDataSubscriptionDetailsModel? Subscription { get; set; } = null;

        public List<UserBillingDataFeatureAddonModel> ActiveFeatureAddons { get; set; } = new List<UserBillingDataFeatureAddonModel>();

        public UserBillingDataCurrentBillingCycleUsageModel CurrentCycleUsage { get; set; } = new UserBillingDataCurrentBillingCycleUsageModel();

        public UserBillingDataAutoRefillSettingsModel AutoRefill { get; set; } = new UserBillingDataAutoRefillSettingsModel();
    }
}

using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataModel
    {
        public UserBillingDataModel() { }
        public UserBillingDataModel (UserBillingData userBillingData)
        {
            CreditBalance = userBillingData.CreditBalance;
            UnpaidOverageBalance = userBillingData.UnpaidOverageBalance;
            Subscription = userBillingData.Subscription != null ? new UserBillingDataSubscriptionDetailsModel(userBillingData.Subscription) : null;
            AdditionalConcurrencySlots = userBillingData.AdditionalConcurrencySlots
                .Select(slot => new UserBillingDataPurchasedConcurrencySlotModel(slot))
                .ToList();
            Usage = new UserBillingDataCurrentBillingCycleUsageModel(userBillingData.Usage);
            AutoRefill = new UserBillingDataAutoRefillSettingsModel(userBillingData.AutoRefill);
        }

        public decimal CreditBalance { get; set; } = 0.00m;
        public decimal UnpaidOverageBalance { get; set; } = 0.00m;

        public UserBillingDataSubscriptionDetailsModel? Subscription { get; set; } = null;

        public List<UserBillingDataPurchasedConcurrencySlotModel> AdditionalConcurrencySlots { get; set; } = new List<UserBillingDataPurchasedConcurrencySlotModel>();

        public UserBillingDataCurrentBillingCycleUsageModel Usage { get; set; } = new UserBillingDataCurrentBillingCycleUsageModel();

        public UserBillingDataAutoRefillSettingsModel AutoRefill { get; set; } = new UserBillingDataAutoRefillSettingsModel();
    }
}

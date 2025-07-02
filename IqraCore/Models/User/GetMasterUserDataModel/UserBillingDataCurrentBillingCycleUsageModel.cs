using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataCurrentBillingCycleUsageModel
    {
        public UserBillingDataCurrentBillingCycleUsageModel() { }
        public UserBillingDataCurrentBillingCycleUsageModel(UserBillingCurrentBillingCycleUsage usage)
        {
            MinutesUsed = usage.MinutesUsed;
            LastResetAt = usage.LastResetAt;
        }

        public decimal MinutesUsed { get; set; } = 0.00m;
        public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
    }
}

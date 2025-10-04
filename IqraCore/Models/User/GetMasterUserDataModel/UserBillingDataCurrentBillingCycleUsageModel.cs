using IqraCore.Attributes;
using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataCurrentBillingCycleUsageModel
    {
        public UserBillingDataCurrentBillingCycleUsageModel() { }
        public UserBillingDataCurrentBillingCycleUsageModel(UserBillingCycleUsage usage)
        {
            CurrentUsage = usage.CurrentUsage;
            LastResetAt = usage.LastResetAt;
        }

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, decimal> CurrentUsage { get; set; } = new Dictionary<string, decimal>();
        public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
    }
}

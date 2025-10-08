using IqraCore.Attributes;
using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataCurrentBillingCycleUsageModel
    {
        public UserBillingDataCurrentBillingCycleUsageModel() { }
        public UserBillingDataCurrentBillingCycleUsageModel(UserBillingCycleUsage usage)
        {
            CurrentUsage = new Dictionary<string, decimal>();
            foreach (var item in usage.CurrentFeatureUsage)
            {
                CurrentUsage.Add(item.Key, item.Value);
            }
            foreach (var item in usage.CurrentConcurrencyFeatureUsage)
            {
                CurrentUsage.Add(item.Key, item.Value.Count);
            }

            LastResetAt = usage.LastResetAt;
        }

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, decimal> CurrentUsage { get; set; } = new Dictionary<string, decimal>();
        public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
    }
}

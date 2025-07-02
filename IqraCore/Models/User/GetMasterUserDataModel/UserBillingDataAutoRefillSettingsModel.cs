using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataAutoRefillSettingsModel
    {
        public UserBillingDataAutoRefillSettingsModel() { }
        public UserBillingDataAutoRefillSettingsModel(UserBillingAutoRefillSettings settings)
        {
            Status = settings.Status;
            RefillWhenBalanceBelow = settings.RefillWhenBalanceBelow;
            RefillAmount = settings.RefillAmount;
            DefaultPaymentMethodId = settings.DefaultPaymentMethodId;
            LastAttemptTimestamp = settings.LastAttemptTimestamp;
            LastAttemptStatusMessage = settings.LastAttemptStatusMessage;
        }

        public UserBillingAutoRefillStatusEnum Status { get; set; } = UserBillingAutoRefillStatusEnum.Disabled;

        public decimal? RefillWhenBalanceBelow { get; set; } = null;
        public decimal? RefillAmount { get; set; } = null;

        public string? DefaultPaymentMethodId { get; set; } = null;
        public DateTime? LastAttemptTimestamp { get; set; } = null;
        public string? LastAttemptStatusMessage { get; set; } = null;
    }
}

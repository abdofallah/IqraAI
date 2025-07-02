using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataSubscriptionDetailsModel
    {
        public UserBillingDataSubscriptionDetailsModel() { }
        public UserBillingDataSubscriptionDetailsModel(UserBillingSubscriptionDetails subscription)
        {
            PlanId = subscription.PlanId;
            Status = subscription.Status;
            CurrentPeriodStart = subscription.CurrentPeriodStart;
            CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            SubscribedAt = subscription.SubscribedAt;
            CanceledAt = subscription.CanceledAt;
        }

        public string PlanId { get; set; } = string.Empty;

        public UserBillingSubscriptionStatusEnum Status { get; set; } = UserBillingSubscriptionStatusEnum.Inactive;

        public DateTime? CurrentPeriodStart { get; set; } = null;

        public DateTime? CurrentPeriodEnd { get; set; } = null;

        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CanceledAt { get; set; } = null;
    }
}

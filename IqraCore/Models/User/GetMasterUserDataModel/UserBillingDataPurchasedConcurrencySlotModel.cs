using IqraCore.Entities.User.Billing;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataPurchasedConcurrencySlotModel
    {
        public UserBillingDataPurchasedConcurrencySlotModel() { }
        public UserBillingDataPurchasedConcurrencySlotModel(UserBillingPurchasedConcurrencySlot slot)
        {
            Id = slot.Id;
            PurchasedAt = slot.PurchasedAt;
            NextBillingDate = slot.NextBillingDate;
            Status = slot.Status;
        }

        public string Id { get; set; } = string.Empty;

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
        public DateTime NextBillingDate { get; set; } = DateTime.UtcNow;

        public UserBillingConcurrencySlotStatusEnum Status { get; set; } = UserBillingConcurrencySlotStatusEnum.Active;
    }
}

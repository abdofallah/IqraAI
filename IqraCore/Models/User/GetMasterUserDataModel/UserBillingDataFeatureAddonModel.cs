using IqraCore.Entities.User.Billing;
using IqraCore.Entities.User.Billing.Enums;

namespace IqraCore.Models.User.Billing
{
    public class UserBillingDataFeatureAddonModel
    {
        public UserBillingDataFeatureAddonModel() { }

        public UserBillingDataFeatureAddonModel(UserBillingFeatureAddon addon)
        {
            Id = addon.Id;
            FeatureKey = addon.FeatureKey;
            Quantity = addon.Quantity;
            PurchasedAt = addon.PurchasedAt;
            NextBillingDate = addon.NextBillingDate;
            Status = addon.Status;
        }

        public string Id { get; set; }

        public string FeatureKey { get; set; }

        public decimal Quantity { get; set; }

        public DateTime PurchasedAt { get; set; }
        public DateTime NextBillingDate { get; set; }
        public UserBillingAddonStatusEnum Status { get; set; }
    }
}
using IqraCore.Entities.User.Billing;

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
            PurchaseValidUntil = addon.PurchaseValidUntil;
            CancelledAt = addon.CancelledAt;
        }

        public string Id { get; set; }

        public string FeatureKey { get; set; }

        public decimal Quantity { get; set; }

        public DateTime PurchasedAt { get; set; }
        public DateTime PurchaseValidUntil { get; set; }

        public DateTime? CancelledAt { get; set; }
    }
}
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace IqraCore.Entities.User
{
    public class UserBillingData
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CreditBalance { get; set; } = 0.00m;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal UnpaidOverageBalance { get; set; } = 0.00m;

        // Only if on a subscription plan, null if the user is on a default/inactive plan.
        public SubscriptionDetails? Subscription { get; set; } = null;

        public List<PurchasedConcurrencySlot> AdditionalConcurrencySlots { get; set; } = new List<PurchasedConcurrencySlot>();

        public CurrentBillingCycleUsage Usage { get; set; } = new CurrentBillingCycleUsage();

        public AutoRefillSettings AutoRefill { get; set; } = new AutoRefillSettings();
    }

    public class SubscriptionDetails
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PlanId { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Inactive;

        public DateTime? CurrentPeriodStart { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        public DateTime SubscribedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
    }

    public class PurchasedConcurrencySlot
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public DateTime PurchasedAt { get; set; }

        public DateTime NextBillingDate { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PriceWhenPurchased { get; set; }

        public ConcurrencySlotStatus Status { get; set; } = ConcurrencySlotStatus.Active;
    }

    public class CurrentBillingCycleUsage
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MinutesUsed { get; set; } = 0.00m;

        public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
    }

    public class AutoRefillSettings
    {
        public AutoRefillStatus Status { get; set; } = AutoRefillStatus.Disabled;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? RefillWhenBalanceBelow { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? RefillAmount { get; set; }

        public string? DefaultPaymentMethodId { get; set; }
        public DateTime? LastAttemptTimestamp { get; set; }
        public string? LastAttemptStatusMessage { get; set; }
    }

    public enum SubscriptionStatus
    {
        Inactive,
        Active,
        PastDue,
        Canceled
    }

    public enum ConcurrencySlotStatus
    {
        Active,
        PastDue,
        Canceled
    }

    public enum AutoRefillStatus
    {
        Disabled,
        Enabled
    }
}
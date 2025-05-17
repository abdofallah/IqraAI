using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using IqraCore.Entities.Helper.Billing;

namespace IqraCore.Entities.User
{
    public class UserBillingData
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CreditBalance { get; set; } = 0.00m;

        [BsonRepresentation(BsonType.ObjectId)]
        public string? CurrentPlanId { get; set; }

        public int PurchasedAdditionalConcurrencySlots { get; set; } = 0;

        public AutoRefillSettings AutoRefill { get; set; } = new AutoRefillSettings();

        public DateTime? LastConcurrencyFeeBilledAt { get; set; }
        public DateTime? NextConcurrencyFeeBillingDate { get; set; }
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
}

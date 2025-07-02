using IqraCore.Entities.Payment;
using IqraCore.Entities.PaymentMethods;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User
{
    public class UserPaymentMethod
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public PaymentProviderTypeEnum PaymentProviderType { get; set; } = PaymentProviderTypeEnum.Unknown;
        public PaymentMethodTypeEnum PaymentMethodType { get; set; } = PaymentMethodTypeEnum.Unknown;

        public bool IsPrimary { get; set; } = false;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}

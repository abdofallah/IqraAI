using IqraCore.Entities.Payment;
using IqraCore.Entities.User.PaymentMethod;

namespace IqraCore.Models.User
{
    public class UserPaymentMethodModel
    {
        public UserPaymentMethodModel() { }
        public UserPaymentMethodModel(UserPaymentMethod userPaymentMethod)
        {
            Id = userPaymentMethod.Id;
            DisplayName = userPaymentMethod.DisplayName;
            PaymentProviderType = userPaymentMethod.PaymentProviderType;
        }

        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public PaymentProviderTypeEnum PaymentProviderType { get; set; } = PaymentProviderTypeEnum.Unknown;
    }
}

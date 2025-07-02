using IqraCore.Entities.Payment;
using IqraCore.Entities.PaymentMethods;
using IqraCore.Entities.User;

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
            PaymentMethodType = userPaymentMethod.PaymentMethodType;
        }

        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public PaymentProviderTypeEnum PaymentProviderType { get; set; } = PaymentProviderTypeEnum.Unknown;
        public PaymentMethodTypeEnum PaymentMethodType { get; set; } = PaymentMethodTypeEnum.Unknown;
    }
}

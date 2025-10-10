using IqraCore.Entities.Payment;
using IqraCore.Entities.User.PaymentMethod;

namespace IqraCore.Models.User.GetMasterUserDataModel
{
    public class UserPaymentMethodModel
    {
        public UserPaymentMethodModel() { }
        public UserPaymentMethodModel(UserPaymentMethod userPaymentMethod)
        {
            Id = userPaymentMethod.Id;
            DisplayName = userPaymentMethod.DisplayName;
            PaymentProviderType = userPaymentMethod.PaymentProviderType;
            AddedAt = userPaymentMethod.AddedAt;
            IsPrimary = userPaymentMethod.IsPrimary;
            HolderName = userPaymentMethod.HolderName;
        }

        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public PaymentProviderTypeEnum PaymentProviderType { get; set; } = PaymentProviderTypeEnum.Unknown;
        public DateTime AddedAt { get; set; }
        public bool IsPrimary { get; set; } = false;
    }
}

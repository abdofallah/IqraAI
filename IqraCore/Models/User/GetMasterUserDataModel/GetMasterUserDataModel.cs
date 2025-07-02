using IqraCore.Entities.User;
using IqraCore.Models.User.Billing;

namespace IqraCore.Models.User.GetMasterUserDataModel
{
    public class GetMasterUserDataModel
    {
        public GetMasterUserDataModel() { }
        public GetMasterUserDataModel(UserData userData)
        {
            Email = userData.Email;
            FirstName = userData.FirstName;
            LastName = userData.LastName;

            Bussiness = userData.Businesses;

            BusinessPermission = new UserPermissionBusinessModel(userData.Permission.Business);

            string? primaryPaymentMethodId = null;
            PaymentMethods = userData.PaymentMethods.Select((pm) =>
            {
                if (pm.IsPrimary) 
                {
                    primaryPaymentMethodId = pm.Id;
                }

                return new UserPaymentMethodModel(pm);
            }).ToList();
            PrimaryPaymentMethodId = primaryPaymentMethodId;

            Billing = new UserBillingDataModel(userData.Billing);
        }

        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public List<long> Bussiness { get; set; } = new List<long>();

        public UserPermissionBusinessModel BusinessPermission { get; set; } = new UserPermissionBusinessModel();

        public List<UserPaymentMethodModel> PaymentMethods { get; set; } = new List<UserPaymentMethodModel>();
        public string? PrimaryPaymentMethodId { get; set; } = null;

        public UserBillingDataModel Billing { get; set; } = new UserBillingDataModel();
    }
}

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

            PaymentMethods = userData.PaymentMethods.Select(x => new UserPaymentMethodModel(x)).ToList();

            Billing = new UserBillingDataModel(userData.Billing);

            ApiKeys = userData.UserApiKeys.Select(x => new UserApiKeyModel(x)).ToList();
        }

        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public List<long> Bussiness { get; set; } = new List<long>();

        public UserPermissionBusinessModel BusinessPermission { get; set; } = new UserPermissionBusinessModel();

        public List<UserPaymentMethodModel> PaymentMethods { get; set; } = new List<UserPaymentMethodModel>();

        public UserBillingDataModel Billing { get; set; } = new UserBillingDataModel();

        public List<UserApiKeyModel> ApiKeys { get; set; } = new List<UserApiKeyModel>();
    }
}

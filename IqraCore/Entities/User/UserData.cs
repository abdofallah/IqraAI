using IqraCore.Attributes;
using IqraCore.Entities.User.Billing;
using IqraCore.Entities.User.PaymentMethod;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User
{
    public class UserData
    {
        [BsonId]
        public string Email { get; set; } = string.Empty;
        public string EmailHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<long> Businesses { get; set; } = new List<long>();

        public List<UserApiKey> UserApiKeys { get; set; } = new List<UserApiKey>();

        public UserPermission Permission { get; set; } = new UserPermission();
        public List<UserPaymentMethod> PaymentMethods { get; set; } = new List<UserPaymentMethod>();
        public UserBillingData Billing { get; set; } = new UserBillingData();

        [ExcludeInAllEndpoints]
        public string PasswordSHA { get; set; } = string.Empty;

        [ExcludeInAllEndpoints]
        public List<UserResetPassword> ResetPasswordTokens { get; set; } = new List<UserResetPassword>();

        [ExcludeInAllEndpoints]
        public string? VerifyEmailToken { get; set; } = null;

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/users")]
        public UserAnalytics Analytics { get; set; } = new UserAnalytics();
    }
}

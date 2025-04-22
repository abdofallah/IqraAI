using IqraCore.Attributes;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User
{
    public class UserData
    {
        [BsonId]
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public List<long> Businesses { get; set; } = new List<long>();

        public UserPermission Permission { get; set; } = new UserPermission();

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

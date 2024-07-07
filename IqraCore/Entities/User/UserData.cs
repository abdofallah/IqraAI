using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.User
{
    public class UserData
    {
        [BsonId]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordSHA { get; set; }
        public List<long> Businesses { get; set; } = new List<long>();
        public List<long> Numbers { get; set; } = new List<long>();
        public UserAnalytics Analytics { get; set; } = new UserAnalytics();
        public string? ResetPasswordToken { get; set; } = null;
        public DateTime? ResetPasswordExpiry { get; set; } = DateTime.UtcNow;
        public UserPermission Permission { get; set; } = new UserPermission();
    }
}

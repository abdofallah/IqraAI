using IqraCore.Attributes;
using MongoDB.Bson;

namespace IqraCore.Entities.User
{
    [ExcludeInAllEndpoints]
    public class UserResetPassword
    {
        public string? RequestedBy { get; set; } = null;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Token { get; set; } = ObjectId.GenerateNewId().ToString();
        public bool IsUsed { get; set; } = true;
    }
}

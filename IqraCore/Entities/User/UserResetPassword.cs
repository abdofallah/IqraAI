namespace IqraCore.Entities.User
{
    public class UserResetPassword
    {
        public string RequestedBy { get; set; } = "Server";
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Token { get; set; } = Guid.NewGuid().ToString();
    }
}

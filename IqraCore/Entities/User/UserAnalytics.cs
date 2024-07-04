namespace IqraCore.Entities.User
{
    public class UserAnalytics
    {
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; } = null;
        public int LoginCount { get; set; } = 0;
    }
}

namespace IqraCore.Entities.User
{
    public class UserAnalytics
    {
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; } = null;
        public int LoginCount { get; set; } = 0;

        public List<UserLoginEntry> LoginHistory { get; set; } = new List<UserLoginEntry>();
    }

    public class UserLoginEntry
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }
}

using IqraCore.Entities.User.Notifcation.Enum;

namespace IqraCore.Entities.User.Notifcation
{
    public class UserNotificationData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public UserNotificationTypeEnum Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsFixed { get; set; } = false;
        public List<UserNotificationActionData> Actions { get; set; } = new List<UserNotificationActionData>();

        public DateTime? ReadOn { get; set; }
        public TimeSpan? OnReadDeleteAfter { get; set; }

        public TimeSpan? DeleteAfter { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserNotificationActionData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string CallbackJavascript { get; set; } = string.Empty;

        public DateTime? ClickedOn { get; set; }
    }
}

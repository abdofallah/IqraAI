using IqraCore.Entities.User.Notifcation;
using IqraCore.Entities.User.Notifcation.Enum;

namespace IqraCore.Models.User.GetMasterUserDataModel
{
    public class UserNotificationDataModel
    {
        public UserNotificationDataModel() { }

        public UserNotificationDataModel(UserNotificationData data)
        {
            Id = data.Id;
            Type = data.Type;
            Title = data.Title;
            Body = data.Body;
            IsFixed = data.IsFixed;
            Actions = data.Actions.Select(
                x => new UserNotificationActionDataModel() {
                    Id = x.Id,
                    Title = x.Title,
                    CallbackJavascript = x.CallbackJavascript
                }
            ).ToList();
            IsRead = data.ReadOn != null;
            CreatedAt = data.CreatedAt;
        }

        public string Id { get; set; }
        public UserNotificationTypeEnum Type { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsFixed { get; set; }
        public List<UserNotificationActionDataModel> Actions { get; set; } = new List<UserNotificationActionDataModel>();

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class UserNotificationActionDataModel
    {
        public string Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CallbackJavascript { get; set; } = string.Empty;
    }
}

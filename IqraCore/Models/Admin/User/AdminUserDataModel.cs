using IqraCore.Attributes;
using IqraCore.Entities.User;
using IqraCore.Entities.User.Notifcation;
using IqraCore.Models.User.MasterUserDataModel;

namespace IqraCore.Models.Admin.User
{
    public class AdminUserDataModel
    {
        public AdminUserDataModel() { }
        public AdminUserDataModel(UserData data)
        {
            Email = data.Email;
            FirstName = data.FirstName;
            LastName = data.LastName;
            Businesses = data.Businesses;
            UserApiKeys = data.UserApiKeys.Select(x => new UserApiKeyModel(x)).ToList();
            Permission = data.Permission;
            Notifications = data.Notifications;
            Analytics = data.Analytics;
        }

        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public List<long> Businesses { get; set; } = new List<long>();
        public List<UserApiKeyModel> UserApiKeys { get; set; } = new List<UserApiKeyModel>();
        public UserPermission Permission { get; set; } = new UserPermission();
        public List<UserNotificationData> Notifications { get; set; } = new List<UserNotificationData>();

        [ExcludeInAllEndpoints]
        [IncludeInEndpoint("/app/admin/users")]
        public UserAnalytics Analytics { get; set; } = new UserAnalytics();
    }
}

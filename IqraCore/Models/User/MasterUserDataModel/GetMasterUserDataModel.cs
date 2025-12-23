using IqraCore.Entities.User;

namespace IqraCore.Models.User.MasterUserDataModel
{
    public class GetMasterUserDataModel
    {
        public GetMasterUserDataModel() { }
        public GetMasterUserDataModel(UserData userData)
        {
            Email = userData.Email;
            FirstName = userData.FirstName;
            LastName = userData.LastName;

            Bussiness = userData.Businesses;

            BusinessPermission = new UserPermissionBusinessModel(userData.Permission.Business);

            ApiKeys = userData.UserApiKeys.Select(x => new UserApiKeyModel(x)).ToList();

            Notifications = userData.Notifications
                .Where(n =>
                {
                    if (n.DeleteAfter.HasValue)
                    {
                        return n.CreatedAt.Add(n.DeleteAfter.Value) > DateTime.UtcNow;
                    }

                    if (!n.ReadOn.HasValue)
                    {
                        return true;
                    }

                    if (!n.OnReadDeleteAfter.HasValue)
                    {
                        return true;
                    }

                    return n.ReadOn.Value.Add(n.OnReadDeleteAfter.Value) > DateTime.UtcNow;
                }
            )
            .Select(x => new UserNotificationDataModel(x))
            .ToList();
        }

        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public List<long> Bussiness { get; set; } = new List<long>();

        public UserPermissionBusinessModel BusinessPermission { get; set; } = new UserPermissionBusinessModel();

        public List<UserApiKeyModel> ApiKeys { get; set; } = new List<UserApiKeyModel>();

        public List<UserNotificationDataModel> Notifications { get; set; } = new List<UserNotificationDataModel>();
    
    }
}

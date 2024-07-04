using IqraCore.Entities.User;

namespace IqraCore.Entities.User
{
    public class UserPermission
    {
        public bool IsAdmin { get; set; } = false;
        public bool CanLogin { get; set; } = true;
        public UserPermissionBusiness Business { get; set; } = new UserPermissionBusiness();
    }
}

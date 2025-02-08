using IqraCore.Attributes;

namespace IqraCore.Entities.User
{
    public class UserPermission
    {
        [ExcludeInAllEndpoints]
        public bool IsAdmin { get; set; } = false;

        public DateTime? LoginDisabledAt { get; set; } = null;
        public string? LoginDisabledReason { get; set; } = null;

        public UserPermissionBusiness Business { get; set; } = new UserPermissionBusiness();
    }
}

using IqraCore.Attributes;

namespace IqraCore.Entities.User
{
    public class UserPermission
    {
        [ExcludeInAllEndpoints]
        public bool IsAdmin { get; set; } = false;

        public DateTime? DisableUserAt { get; set; } = null;
        public string? UserDisabledReason { get; set; } = null;


        public UserWhiteLabelPermission WhiteLabel { get; set; } = new UserWhiteLabelPermission();
        public UserPermissionBusiness Business { get; set; } = new UserPermissionBusiness();
    }

    public class UserWhiteLabelPermission
    {
        public DateTime? DisabledAt { get; set; } = null;
        public string? DisabledReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        public string? DisabledEditingReason { get; set; } = null;
    }
}

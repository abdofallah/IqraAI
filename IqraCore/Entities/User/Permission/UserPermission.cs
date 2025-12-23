using IqraCore.Attributes;

namespace IqraCore.Entities.User
{
    public class UserPermission
    {
        [ExcludeInAllEndpoints]
        public bool IsAdmin { get; set; } = false;

        public DateTime? DisableUserAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? UserDisabledPrivateReason { get; set; } = null;
        public string? UserDisabledPublicReason { get; set; } = null;

        public UserWhiteLabelPermission WhiteLabel { get; set; } = new UserWhiteLabelPermission();
        public UserPermissionBusiness Business { get; set; } = new UserPermissionBusiness();
    }

    public class UserWhiteLabelPermission
    {
        public DateTime? DisabledAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? DisabledPrivateReason { get; set; } = null;
        public string? DisabledPublicReason { get; set; } = null;

        public DateTime? DisabledEditingAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? DisabledEditingPrivateReason { get; set; } = null;
        public string? DisabledEditingPublicReason { get; set; } = null;
    }
}

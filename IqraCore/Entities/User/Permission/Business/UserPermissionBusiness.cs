using IqraCore.Attributes;

namespace IqraCore.Entities.User
{
    public class UserPermissionBusiness 
    {
        public DateTime? DisableBusinessesAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? DisableBusinessesPrivateReason { get; set; } = null;
        public string? DisableBusinessesPublicReason { get; set; } = null;

        public DateTime? AddBusinessDisabledAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? AddBusinessDisablePrivateReason { get; set; } = null;
        public string? AddBusinessDisablePublicReason { get; set; } = null;

        public DateTime? EditBusinessDisabledAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? EditBusinessDisablePrivateReason { get; set; } = null;
        public string? EditBusinessDisablePublicReason { get; set; } = null;

        public DateTime? DeleteBusinessDisableAt { get; set; } = null;
        [ExcludeInAllEndpoints]
        public string? DeleteBusinessDisablePrivateReason { get; set; } = null;
        public string? DeleteBusinessDisablePublicReason { get; set; } = null;
    }
}

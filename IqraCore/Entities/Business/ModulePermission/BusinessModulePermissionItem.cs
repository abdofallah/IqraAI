using IqraCore.Attributes;

namespace IqraCore.Entities.Business.ModulePermission
{
    public class BusinessModulePermissionItem
    {
        public DateTime? DisabledAt { get; set; }
        [ExcludeInAllEndpoints]
        public string? PrivateReason { get; set; }
        public string? PublicReason { get; set; }
    }
}

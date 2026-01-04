using IqraCore.Entities.Business.ModulePermission.ENUM;

namespace IqraCore.Entities.Validation
{
    public class ModulePermissionCheckData
    {
        public string ModulePath { get; set; } = string.Empty;
        public BusinessModulePermissionType Type { get; set; }
    }
}

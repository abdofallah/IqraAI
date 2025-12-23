using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;

namespace IqraCore.Interfaces.Validation
{
    public interface IUserBusinessPermissionHelper
    {
        FunctionReturnResult CheckBusinessPermission(BusinessData businessData, bool checkBusinessIsDisabled, bool checkBusinessCanBeEdited, bool checkBusinessCanBeDeleted);
        FunctionReturnResult CheckBusinessModulePermission(BusinessModulePermission rootPermission, ModulePermissionCheckData check);

        public class ModulePermissionCheckData
        {
            public string ModulePath { get; set; } = string.Empty;
            public BusinessModulePermissionType Type { get; set; }
        }
    }
}

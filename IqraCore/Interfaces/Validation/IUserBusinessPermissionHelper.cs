using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Validation;

namespace IqraCore.Interfaces.Validation
{
    public partial interface IUserBusinessPermissionHelper
    {
        FunctionReturnResult CheckBusinessPermission(BusinessData businessData, bool checkBusinessIsDisabled, bool checkBusinessCanBeEdited, bool checkBusinessCanBeDeleted);
        FunctionReturnResult CheckBusinessModulePermission(BusinessModulePermission rootPermission, ModulePermissionCheckData check);
    }
}

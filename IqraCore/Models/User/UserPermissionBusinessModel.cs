using IqraCore.Entities.User;

namespace IqraCore.Models.User
{
    public class UserPermissionBusinessModel
    {
        public UserPermissionBusinessModel() { }
        public UserPermissionBusinessModel(UserPermissionBusiness userPermissionBusiness)
        {
            DisableBusinesses = userPermissionBusiness.DisableBusinessesAt.HasValue;
            DisableBusinessesReason = userPermissionBusiness.DisableBusinessesReason;

            AddBusinessDisabled = userPermissionBusiness.AddBusinessDisabledAt.HasValue;
            AddBusinessDisableReason = userPermissionBusiness.AddBusinessDisableReason;

            EditBusinessDisabled = userPermissionBusiness.EditBusinessDisabledAt.HasValue;
            EditBusinessDisableReason = userPermissionBusiness.EditBusinessDisableReason;

            DeleteBusinessDisable = userPermissionBusiness.DeleteBusinessDisableAt.HasValue;
            DeleteBusinessDisableReason = userPermissionBusiness.DeleteBusinessDisableReason;
        }

        public bool DisableBusinesses { get; set; } = false;
        public string? DisableBusinessesReason { get; set; } = null;

        public bool AddBusinessDisabled { get; set; } = false;
        public string? AddBusinessDisableReason { get; set; } = null;

        public bool EditBusinessDisabled { get; set; } = false;
        public string? EditBusinessDisableReason { get; set; } = null;

        public bool DeleteBusinessDisable { get; set; } = false;
        public string? DeleteBusinessDisableReason { get; set; } = null;
    }
}

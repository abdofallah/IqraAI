namespace IqraCore.Entities.User
{
    public class UserPermissionBusiness
    {
        public bool CanViewBusinesses { get; set; } = true;
        public bool CanAddBusinesses { get; set; } = true;
        public bool CanEditBusinesses { get; set; } = true;
        public bool CanDeleteBusinesses { get; set; } = true;
    }
}

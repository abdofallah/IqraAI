namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionSettings
    {
        public bool TabEnabled { get; set; } = true;

        public BusinessUserPermissionSettingsGeneral General { get; set; } = new BusinessUserPermissionSettingsGeneral();
        public BusinessUserPermissionSettingsLanguages Languages { get; set; } = new BusinessUserPermissionSettingsLanguages();
        public BusinessUserPermissionSettingsUsers Users { get; set; } = new BusinessUserPermissionSettingsUsers();
    }

    public class BusinessUserPermissionSettingsGeneral
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
    }

    public class BusinessUserPermissionSettingsLanguages
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
        public bool Add { get; set; } = true;
        public bool Delete { get; set; } = true;
    }

    public class BusinessUserPermissionSettingsUsers
    {
        public bool TabEnabled { get; set; } = true;
        public bool Edit { get; set; } = true;
        public bool Add { get; set; } = true;
        public bool Delete { get; set; } = true;
    }
}

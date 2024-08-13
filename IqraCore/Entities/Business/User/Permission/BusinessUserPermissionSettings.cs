namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionSettings
    {
        public bool TabEnabled { get; set; } = false;

        public BusinessUserPermissionSettingsGeneral General { get; set; } = new BusinessUserPermissionSettingsGeneral();
        public BusinessUserPermissionSettingsLanguages Languages { get; set; } = new BusinessUserPermissionSettingsLanguages();
        public BusinessUserPermissionSettingsUsers Users { get; set; } = new BusinessUserPermissionSettingsUsers();
        public BusinessUserPermissionSettingsDomain Domain { get; set; } = new BusinessUserPermissionSettingsDomain();
    }

    public class BusinessUserPermissionSettingsGeneral
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
    }

    public class BusinessUserPermissionSettingsLanguages
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }

    public class BusinessUserPermissionSettingsUsers
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }

    public class BusinessUserPermissionSettingsDomain
    {
        public bool TabEnabled { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Delete { get; set; } = false;
    }
}

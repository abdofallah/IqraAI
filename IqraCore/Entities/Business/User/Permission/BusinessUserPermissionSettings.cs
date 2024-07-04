namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionSettings
    {
        public bool SettingsTabEnabled { get; set; } = true;

    }

    public class BusinessUserPermissionSettingsGeneral
    {
        public bool GeneralTabEnabled { get; set; } = true;
        public bool EditGeneral { get; set; } = true;
    }

    public class BusinessUserPermissionSettingsLanguages
    {
        public bool LanguagesTabEnabled { get; set; } = true;
        public bool EditLanguages { get; set; } = true;
        public bool AddLanguages { get; set; } = true;
        public bool DeleteLanguages { get; set; } = true;
        public int MaxAllowedLanguages { get; set; } = -1;
    }

    public class BusinessUserPermissionSettingsRegion
    {
        public bool RegionTabEnabled { get; set; } = true;
        public bool EditRegion { get; set; } = true;
    }

    public class BusinessUserPermissionSettingsUsers
    {
        public bool UsersTabEnabled { get; set; } = true;
        public bool EditUsers { get; set; } = true;
        public bool AddUsers { get; set; } = true;
        public bool DeleteUsers { get; set; } = true;
        public int MaxAllowedUsers { get; set; } = -1;
    }
}

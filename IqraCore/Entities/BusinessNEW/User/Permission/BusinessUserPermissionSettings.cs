namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUserPermissionSettings
    {
        public bool SettingsTabEnabled { get; set; }

    }

    public class BusinessUserPermissionSettingsGeneral
    {
        public bool GeneralTabEnabled { get; set; }
        public bool EditGeneral { get; set; }
    }

    public class BusinessUserPermissionSettingsLanguages
    {
        public bool LanguagesTabEnabled { get; set; }
        public bool EditLanguages { get; set; }
        public bool AddLanguages { get; set; }
        public bool DeleteLanguages { get; set; }
        public int MaxAllowedLanguages { get; set; }
    }

    public class BusinessUserPermissionSettingsRegion
    {
        public bool RegionTabEnabled { get; set; }
        public bool EditRegion { get; set; }
    }

    public class BusinessUserPermissionSettingsUsers
    {
        public bool UsersTabEnabled { get; set; }
        public bool EditUsers { get; set; }
        public bool AddUsers { get; set; }
        public bool DeleteUsers { get; set; }
        public int MaxAllowedUsers { get; set; }
    }
}

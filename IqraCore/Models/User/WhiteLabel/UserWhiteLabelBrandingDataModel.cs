using IqraCore.Entities.User.WhiteLabel;

namespace IqraCore.Models.User.WhiteLabel
{
    public class UserWhiteLabelBrandingDataModel
    {
        public UserWhiteLabelBrandingDataModel() { }
        public UserWhiteLabelBrandingDataModel (UserWhiteLabelBrandingData data)
        {
            PlatformName = data.PlatformName;
            PlatformLogo = data.PlatformLogo;
            PlatformIcon = data.PlatformIcon;
            PlatformCustomCSS = data.PlatformCustomCSS;
        }

        public string PlatformName { get; set; } = string.Empty;
        public string PlatformLogo { get; set; } = string.Empty;
        public string PlatformIcon { get; set; } = string.Empty;
        public string PlatformCustomCSS { get; set; } = string.Empty;
    }
}

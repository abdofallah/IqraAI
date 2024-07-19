using IqraCore.Entities.Helper.Business;

namespace IqraCore.Entities.Business
{
    public class BusinessUserWhiteLabel
    {
        // General
        public string PlatformName { get; set; } = string.Empty;
        public string PlatformTitle { get; set; } = string.Empty;
        public string PlatformDescription { get; set; } = string.Empty;

        // Styles
        public string LogoURL { get; set; } = string.Empty;
        public string FaviconIconURL { get; set; } = string.Empty;
        public string CustomCSS { get; set; } = string.Empty;
        public string CustomJavaScript { get; set; } = string.Empty;

        // Domain
        public long DomainId { get; set; } = -1;
    }
}

using IqraCore.Entities.User.WhiteLabel;

namespace IqraCore.Models.User.WhiteLabel
{
    public class UserWhiteLabelDomainDataModel
    {
        public UserWhiteLabelDomainDataModel() { }
        public UserWhiteLabelDomainDataModel (UserWhiteLabelDomainData data)
        {
            CustomDomain = data.CustomDomain;
            OverrideBranding = new UserWhiteLabelBrandingDataModel(data.OverrideBranding);
            UseCustomSSL = data.UseCustomSSL;
            SSLPrivateKey = data.SSLPrivateKey;
            SSLCertificate = data.SSLCertificate;
        }

        public string CustomDomain { get; set; } = string.Empty;
        public UserWhiteLabelBrandingDataModel OverrideBranding { get; set; } = new UserWhiteLabelBrandingDataModel();

        // Custom SSL
        public DateTime? UseCustomSSL { get; set; } = null;
        public string? SSLPrivateKey { get; set; } = null;
        public string? SSLCertificate { get; set; } = null;
    }
}

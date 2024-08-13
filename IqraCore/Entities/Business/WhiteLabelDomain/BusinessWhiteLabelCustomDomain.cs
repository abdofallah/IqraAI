namespace IqraCore.Entities.Business.WhiteLabelDomain
{
    public  class BusinessWhiteLabelCustomDomain : BusinessWhiteLabelDomain
    {
        public string CustomDomain { get; set; } = string.Empty;
        public DateTime? SSLEnabled { get; set; } = null;
        public bool UseLetsEncryptSSL { get; set; } = true;
        public string? SSLPrivateKey { get; set; } = null;
        public string? SSLCertificate { get; set; } = null;
    }
}

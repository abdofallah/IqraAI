namespace IqraCore.Entities.Business
{
    public class BusinessAppIntegrationOAuthData
    {
        public string? EncryptedRefreshToken { get; set; } = null;
        public string? EncryptedAccessToken { get; set; } = null;
        public DateTime? AccessTokenExpiresAt { get; set; } = null;
        public Dictionary<string, string> ExtraFields { get; set; } = new Dictionary<string, string>();

        public string Status { get; set; } = string.Empty;
        public string? StatusMessage { get; set; } = null;
        public DateTime? StatusCheckedAt { get; set; } = null;

        public const string StatusConnected = "Connected";
        public const string StatusFailed = "Failed";
        public const string StatusNotConnected = "NotConnected";
    }
}

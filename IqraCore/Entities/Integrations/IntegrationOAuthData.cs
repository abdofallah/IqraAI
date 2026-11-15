namespace IqraCore.Entities.Integrations
{
    public class IntegrationOAuthData
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string Scopes { get; set; } = string.Empty;
        public Dictionary<string, string> AdditionalAuthorizationParameters { get; set; } = new Dictionary<string, string>();
        public bool UseBasicAuthForTokenEndpoint { get; set; } = false;
        public List<string> ExtraResponseFields { get; set; } = new List<string>();

        public const string ClientIdField = "ClientId";
        public const string ClientSecretField = "ClientSecret";
        public const string ScopesField = "Scopes";
    }
}

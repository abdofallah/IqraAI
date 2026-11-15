namespace IqraCore.Models.Integrations
{
    public class IntegrationOAuthResultViewModel
    {
        public string RedirectUrl { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

namespace IqraCore.Entities.WhiteLabel
{
    public class WhiteLabelRequestContext
    {
        public bool IsValid { get; set; }
        public string? MasterUserEmail { get; set; }
        public WhiteLabelBrandingData? Branding { get; set; }
    }
}

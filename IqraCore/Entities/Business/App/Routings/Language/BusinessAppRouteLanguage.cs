namespace IqraCore.Entities.Business
{
    public class BusinessAppRouteLanguage
    {
        public string DefaultLanguageCode { get; set; } = string.Empty;
        public bool MultiLanguageEnabled { get; set; } = false;
        public List<BusinessAppRouteLanguageMultiEnabled> EnabledMultiLanguages { get; set; } = new List<BusinessAppRouteLanguageMultiEnabled>();
    }
}

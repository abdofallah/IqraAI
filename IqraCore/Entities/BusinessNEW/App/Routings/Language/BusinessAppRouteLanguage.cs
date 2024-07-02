namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppRouteLanguage
    {
        public string DefaultLanguageCode { get; set; }
        public bool MultiLanguageEnabled { get; set; }
        public List<BusinessAppRouteLanguageMultiEnabled> EnabledMultiLanguages { get; set; }
    }
}

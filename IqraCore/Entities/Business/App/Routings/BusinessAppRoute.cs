namespace IqraCore.Entities.Business
{
    public class BusinessAppRoute
    {
        public string Id { get; set; } = string.Empty;
        public BusinessAppRouteGeneral General { get; set; } = new BusinessAppRouteGeneral();
        public BusinessAppRouteLanguage Language { get; set; } = new BusinessAppRouteLanguage();
        public BusinessAppRouteConfiguration Configuration { get; set; } = new BusinessAppRouteConfiguration();
        public List<string> Numbers { get; set; } = new List<string>();
        public BusinessAppRouteAgent Agent { get; set; } = new BusinessAppRouteAgent();
        public BusinessAppRouteActions Actions { get; set; } = new BusinessAppRouteActions();
    }
}

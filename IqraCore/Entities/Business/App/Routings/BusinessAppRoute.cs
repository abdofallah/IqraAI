namespace IqraCore.Entities.Business
{
    public class BusinessAppRoute
    {
        public BusinessAppRouteGeneral General { get; set; } = new BusinessAppRouteGeneral();
        public BusinessAppRouteLanguage Language { get; set; } = new BusinessAppRouteLanguage();
        public BusinessAppRouteNumber Number { get; set; } = new BusinessAppRouteNumber();
        public BusinessAppRouteAgent Agent { get; set; } = new BusinessAppRouteAgent();
        public BusinessAppRouteActions Actions { get; set; } = new BusinessAppRouteActions();
    }
}

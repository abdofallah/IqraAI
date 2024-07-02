namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppRoute
    {
        public BusinessAppRouteGeneral General { get; set; }
        public BusinessAppRouteLanguage Language { get; set; }
        public BusinessAppRouteNumber Number { get; set; }
        public BusinessAppRouteAgent Agent { get; set; }
        public BusinessAppRouteActions Actions { get; set; }
    }
}

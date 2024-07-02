namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessUserPermissionRouting
    {
        public bool RoutingTabEnabled { get; set; }
        public bool AddNewRoute { get; set; }
        public bool EditRoute { get; set; }
        public bool DeleteRoute { get; set; }
        public int MaxAllowedRoutes { get; set; }
    }
}

namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionRouting
    {
        public bool RoutingTabEnabled { get; set; } = true;
        public bool AddNewRoute { get; set; } = true;
        public bool EditRoute { get; set; } = true;
        public bool DeleteRoute { get; set; } = true;
    }
}

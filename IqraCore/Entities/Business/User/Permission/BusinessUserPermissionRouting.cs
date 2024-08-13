namespace IqraCore.Entities.Business
{
    public class BusinessUserPermissionRouting
    {
        public bool TabEnabled { get; set; } = false;
        public bool Add { get; set; } = false;
        public bool Edit { get; set; } = false;
        public bool Delete { get; set; } = false;
    }
}

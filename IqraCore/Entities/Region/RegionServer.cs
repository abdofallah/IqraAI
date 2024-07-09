namespace IqraCore.Entities.Region
{
    public class RegionServer
    {
        public string IpAddress { get; set; } = string.Empty;
        public bool HasSimModules { get; set; } = false;

        public DateTime? DisabledAt { get; set; } = null;
    }
}

namespace IqraCore.Entities.Server
{
    public class ServerHardwareStatusData
    {
        public List<ServerHardwareStatusItemData> CPUUsage { get; set; } = new List<ServerHardwareStatusItemData>();
        public List<ServerHardwareStatusItemData> RamUsage { get; set; } = new List<ServerHardwareStatusItemData>();
        public List<ServerHardwareStatusItemData> DiskUsage { get; set; } = new List<ServerHardwareStatusItemData>();
        public List<ServerHardwareStatusItemData> NetworkUsage { get; set; } = new List<ServerHardwareStatusItemData>();
    }

    public class ServerHardwareStatusItemData
    {
        public string Identifier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public List<ServerHardwareStatusItemSensorData> Sensors { get; set; } = new List<ServerHardwareStatusItemSensorData>();
    }

    public class ServerHardwareStatusItemSensorData
    {
        public string Name { get; set; } = string.Empty;

        public float? Value { get; set; } = 0;
        public float? Min { get; set; } = 0;
        public float? Max { get; set; } = 0;
    }
}

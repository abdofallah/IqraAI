namespace IqraCore.Entities.Server
{
    public class ServerStatusData
    {
        public int OnGoingCalls { get; set; } = 0;
        public int QueuedCalls { get; set; } = 0;

        public ServerHardwareStatusData HardwareStatus { get; set; } = new ServerHardwareStatusData();
    }
}

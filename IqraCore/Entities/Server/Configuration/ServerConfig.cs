namespace IqraCore.Entities.Server
{
    public class ServerConfig
    {
        public string ServerId { get; set; } = string.Empty;
        public string RegionId { get; set; } = string.Empty;
        public int ExpectedMaxConcurrentCalls { get; set; } = 50;
    }
}

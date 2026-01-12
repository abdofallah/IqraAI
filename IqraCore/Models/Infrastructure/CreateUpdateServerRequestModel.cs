using IqraCore.Entities.Helper.Server;

namespace IqraCore.Models.Infrastructure
{
    public class CreateUpdateServerRequestModel
    {
        public string Endpoint { get; set; } = string.Empty;
        public string APIKey { get; set; } = string.Empty;
        public bool UseSSL { get; set; } = true;
        public int SIPPort { get; set; } = 5060;
        public ServerTypeEnum Type { get; set; }
        public bool IsDevelopmentServer { get; set; }
    }
}
namespace IqraCore.Models.Infrastructure
{
    public class UpdateCoreBackgroundConfigRequestModel
    {
        public string Endpoint { get; set; }
        public bool UseSSL { get; set; }
        public string ApiKey { get; set; }
    }
}

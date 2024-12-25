namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentIntegrationData
    {
        public string Id { get; set; }
        public Dictionary<string, object> FieldValues { get; set; } = new Dictionary<string, object>();
    }
}

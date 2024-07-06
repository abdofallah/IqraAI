namespace IqraCore.Entities.Business
{
    public class BusinessAppToolResponse
    {
        public string Javascript { get; set; } = string.Empty;
        public bool HasStaticResponse { get; set; } = false;
        public Dictionary<string, string>? StaticResponse { get; set; } = null;
    }
}

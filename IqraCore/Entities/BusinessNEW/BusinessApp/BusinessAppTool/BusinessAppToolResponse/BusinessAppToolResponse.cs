namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppToolResponse
    {
        public string Javascript { get; set; }
        public bool HasStaticResponse { get; set; }
        public Dictionary<string, string>? StaticResponse { get; set; }
    }
}

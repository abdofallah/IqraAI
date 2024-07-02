namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppToolConfigurationInputSchemea
    {
        public Dictionary<string, string> Name { get; set; }
        public Dictionary<string, string> Description { get; set; }
        public Type Type { get; set; }
        public bool IsArray { get; set; }
        public bool IsRequired { get; set; }
    }
}

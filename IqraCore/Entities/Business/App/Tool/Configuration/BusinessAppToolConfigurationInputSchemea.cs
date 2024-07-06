namespace IqraCore.Entities.Business
{
    public class BusinessAppToolConfigurationInputSchemea
    {
        public long Id { get; set; }
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Description { get; set; } = new Dictionary<string, string>();
        public Type Type { get; set; } = typeof(string);
        public bool IsArray { get; set; } = false;
        public bool IsRequired { get; set; } = false;
    }
}

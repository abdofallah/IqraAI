namespace IqraCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ExcludeInEndpointAttribute : Attribute
    {
        public string Endpoint { get; }

        public ExcludeInEndpointAttribute(string endpoint)
        {
            Endpoint = endpoint;
        }
    }
}

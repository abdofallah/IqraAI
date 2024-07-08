namespace IqraCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class IncludeInEndpointAttribute : Attribute
    {
        public string Endpoint { get; }

        public IncludeInEndpointAttribute(string endpoint)
        {
            Endpoint = endpoint;
        }
    }
}

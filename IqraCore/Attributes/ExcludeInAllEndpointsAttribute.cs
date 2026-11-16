namespace IqraCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
    public class ExcludeInAllEndpointsAttribute : Attribute
    {
    }
}

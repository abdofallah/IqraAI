namespace IqraCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MultiLanguagePropertyAttribute : Attribute
    {
        public int LanguagesRequired { get; set; } = -1;
    }
}

using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppToolGeneral
    {
        [MultiLanguageProperty]
        public Dictionary<string, string> Name { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, string> ShortDescription { get; set; } = new Dictionary<string, string>();
    }
}

using IqraCore.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheMessageGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [MultiLanguageProperty]
        public Dictionary<string, List<BusinessAppCacheMessage>> Messages { get; set; } = new Dictionary<string, List<BusinessAppCacheMessage>>();

        public List<string> AgentReferences { get; set; } = new List<string>();
    }
}

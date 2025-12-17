using IqraCore.Entities.Business.App.KnowledgeBase.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase
{
    public class BusinessAppKnowledgeBase
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public BusinessAppKnowledgeBaseGeneral General { get; set; } = new BusinessAppKnowledgeBaseGeneral();
        public BusinessAppKnowledgeBaseConfiguration Configuration { get; set; } = new BusinessAppKnowledgeBaseConfiguration();
        public List<long> Documents { get; set; } = new List<long>();

        public List<string> AgentReferences { get; set; } = new List<string>();
    }
}

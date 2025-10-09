using IqraCore.Entities.Business.App.KnowledgeBase.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business.App.KnowledgeBase
{
    public class BusinessAppKnowledgeBase
    {
        [BsonId]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public BusinessAppKnowledgeBaseGeneral General { get; set; } = new BusinessAppKnowledgeBaseGeneral();
        public BusinessAppKnowledgeBaseConfiguration Configuration { get; set; } = new BusinessAppKnowledgeBaseConfiguration();
        public List<long> Documents { get; set; } = new List<long>();
    }

    public class BusinessAppKnowledgeBaseGeneral
    {
        public string Emoji { get; set; } = "🧠";
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

using IqraCore.Entities.Business.App.KnowledgeBase;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessApp
    {
        [BsonId]
        public long Id { get; set; }

        public BusinessAppContext Context { get; set; } = new BusinessAppContext();
        public List<BusinessAppTool> Tools { get; set; } = new List<BusinessAppTool>();
        public List<BusinessAppAgent> Agents { get; set; } = new List<BusinessAppAgent>();
        public List<BusinessAppIntegration> Integrations { get; set; } = new List<BusinessAppIntegration>();
        public BusinessAppCache Cache { get; set; } = new BusinessAppCache();
        public List<BusinessAppRoute> Routings { get; set; } = new List<BusinessAppRoute>();
        public List<BusinessNumberData> Numbers { get; set; } = new List<BusinessNumberData>();
        public List<BusinessAppKnowledgeBase> KnowledgeBases { get; set; } = new List<BusinessAppKnowledgeBase>();
    }
}

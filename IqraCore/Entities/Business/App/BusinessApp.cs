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
        public List<BusinessAppRoute> Routings { get; set; } = new List<BusinessAppRoute>();
    }
}

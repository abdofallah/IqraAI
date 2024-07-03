using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessApp
    {
        [BsonId]
        public long Id { get; set; }

        public BusinessAppContext Context { get; set; }
        public List<BusinessAppTool> Tools { get; set; }
        public List<BusinessAppAgent> Agents { get; set; }
        public List<BusinessAppRoute> Routings { get; set; }
    }
}

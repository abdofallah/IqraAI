using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IqraCore.Entities.Business
{
    public class BusinessAppScript
    {
        [BsonId]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public BusinessAppScriptGeneral General { get; set; } = new BusinessAppScriptGeneral();
        public List<BusinessAppScriptNode> Nodes { get; set; } = new List<BusinessAppScriptNode>();
        public List<BusinessAppScriptEdge> Edges { get; set; } = new List<BusinessAppScriptEdge>();
    }  
}

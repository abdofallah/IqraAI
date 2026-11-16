using IqraCore.Attributes;
using IqraCore.Entities.Business.App.Script;
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
        public List<BusinessAppScriptVariable> Variables { get; set; } = new List<BusinessAppScriptVariable>();

        // Route/Campaigns References
        [ExcludeInAllEndpoints]
        public List<string> InboundRoutingReferences { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> TelephonyCampaignReferences { get; set; } = new List<string>();
        [ExcludeInAllEndpoints]
        public List<string> WebCampaignReferences { get; set; } = new List<string>();

        // Add/Transfer Script Node References
        public List<BusinessAppScriptAddScriptToContextNodeReference> ScriptAddScriptNodeReferences { get; set; } = new List<BusinessAppScriptAddScriptToContextNodeReference>();
    }

    [ExcludeInAllEndpoints]
    public class BusinessAppScriptAddScriptToContextNodeReference
    {
        public string ScriptId { get; set; }
        public string NodeId { get; set; }
    }
}

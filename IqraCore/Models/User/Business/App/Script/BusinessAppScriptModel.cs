using IqraCore.Entities.Business;
using IqraCore.Entities.Business.App.Script;

namespace IqraCore.Models.User.Business.App.Script
{
    public class BusinessAppScriptModel
    {
        public BusinessAppScriptModel(BusinessAppScript data)
        {
            Id = data.Id;
            General = data.General;
            Nodes = data.Nodes;
            Edges = data.Edges;
            Variables = data.Variables;
        }

        public string Id { get; set; }
        public BusinessAppScriptGeneral General { get; set; }
        public List<BusinessAppScriptNode> Nodes { get; set; }
        public List<BusinessAppScriptEdge> Edges { get; set; }
        public List<BusinessAppScriptVariable> Variables { get; set; }
    }
}

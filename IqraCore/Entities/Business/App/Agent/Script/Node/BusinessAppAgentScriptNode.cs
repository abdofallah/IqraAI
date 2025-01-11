using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptNode
    {
        public string Id { get; set; } = "";
        public BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.Unknown;
        public BusinessAppAgentScriptNodePosition Position { get; set; } = new BusinessAppAgentScriptNodePosition();
    }

    public class BusinessAppAgentScriptNodePosition
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}

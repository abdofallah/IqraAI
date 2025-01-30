using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptNode
    {
        public string Id { get; set; } = "";
        public virtual BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.Unknown;
        public BusinessAppAgentScriptNodePosition Position { get; set; } = new BusinessAppAgentScriptNodePosition();
    }

    public class BusinessAppAgentScriptNodePosition
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
    }
}

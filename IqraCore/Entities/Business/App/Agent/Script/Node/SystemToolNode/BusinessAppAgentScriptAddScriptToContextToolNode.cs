using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptAddScriptToContextToolNode : BusinessAppAgentScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.AddScriptToContext;

        public string ScriptId { get; set; } = "";
    }
}

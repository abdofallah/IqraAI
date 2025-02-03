using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptCustomToolNode : BusinessAppAgentScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.ExecuteCustomTool;

        public string ToolId { get; set; } = "";

        [KeepOriginalDictionaryKeyCase]
        public Dictionary<string, string> ToolConfiguration { get; set; } = new Dictionary<string, string>();
    }
}

using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppScriptUserQueryNode : BusinessAppScriptNode
    {
        public override BusinessAppAgentScriptNodeTypeENUM NodeType { get; set; } = BusinessAppAgentScriptNodeTypeENUM.UserQuery;

        [MultiLanguageProperty]
        public Dictionary<string, string> Query { get; set; } = new Dictionary<string, string>();

        [MultiLanguageProperty]
        public Dictionary<string, List<string>> Examples { get; set; } = new Dictionary<string, List<string>>();
    }
}

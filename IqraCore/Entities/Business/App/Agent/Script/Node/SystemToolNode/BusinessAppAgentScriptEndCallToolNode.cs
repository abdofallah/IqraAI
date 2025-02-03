using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptEndCallToolNode : BusinessAppAgentScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.EndCall;

        public BusinessAppAgentScriptEndCallTypeENUM Type { get; set; } = BusinessAppAgentScriptEndCallTypeENUM.Immediate;

        [MultiLanguageProperty]
        public Dictionary<string, string>? Messages { get; set; } = null;
    }
}

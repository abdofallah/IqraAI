using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptTransferToAgentToolNode : BusinessAppAgentScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.TransferToAgent;

        public string AgentId { get; set; } = "";
        public bool TransferConversation { get; set; } = true;
        public bool SummarizeConversation { get; set; } = false;
    }
}

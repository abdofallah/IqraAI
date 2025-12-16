using IqraCore.Attributes;
using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppScriptSendSMSToolNode : BusinessAppScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.SendSMS;

        public string PhoneNumberId { get; set; } = string.Empty;

        [MultiLanguageProperty]
        public Dictionary<string, string>? Messages { get; set; } = null;
    }
}

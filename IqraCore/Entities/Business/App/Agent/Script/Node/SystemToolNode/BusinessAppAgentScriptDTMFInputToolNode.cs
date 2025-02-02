using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScriptDTMFInputToolNode : BusinessAppAgentScriptSystemToolNode
    {
        public override BusinessAppAgentScriptNodeSystemToolTypeENUM ToolType { get; set; } = BusinessAppAgentScriptNodeSystemToolTypeENUM.GetDTMFKeypadInput;

        public int Timeout { get; set; } = 5000;
        public bool RequireStartAsterisk { get; set; } = false;
        public bool RequireEndHash { get; set; } = false;
        public int MaxLength { get; set; } = 1;
        public bool EncryptInput { get; set; } = false;
        public string? VariableName { get; set; } = null;
        public List<BusinessAppAgentScriptDTMFOutcome> Outcomes { get; set; } = new List<BusinessAppAgentScriptDTMFOutcome>();
    }

    public class BusinessAppAgentScriptDTMFOutcome
    {
        public string Value { get; set; } = "";
    }
}

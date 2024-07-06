namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentScript
    {
        public long Id { get; set; }
        public BusinessAppAgentScriptGeneral General { get; set; } = new BusinessAppAgentScriptGeneral();
        public List<BusinessAppAgentScriptConversation> Conversation { get; set; } = new List<BusinessAppAgentScriptConversation>();
    }  
}

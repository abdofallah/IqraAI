namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessAppAgentScript
    {
        public long Id { get; set; }
        public BusinessAppAgentScriptGeneral General { get; set; }
        public List<BusinessAppAgentScriptConversation> Conversation { get; set; }
    }  
}

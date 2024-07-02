namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationConversation
    {
        public string AIAudioURL { get; set; }
        public string UserAudioURL { get; set; }
        public List<BusinessConversationConversationMessage> Messages { get; set; }
    }
}

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAgentEventLLMStreamed
    {
        public ConversationAgentEventLLMStreamed(object respnseObject, bool isCachedResponse = false)
        {
            ResponseObject = respnseObject;
            IsCachedResponse = isCachedResponse;
        }

        public object ResponseObject { get; set; } = null!;
        public bool IsCachedResponse { get; set; } = false;
    }
}

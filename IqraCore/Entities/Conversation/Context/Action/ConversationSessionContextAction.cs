namespace IqraCore.Entities.Conversation.Context.Action
{
    public class ConversationSessionContextAction
    {
        public string? SelectedToolId { get; set; } = null;
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
}

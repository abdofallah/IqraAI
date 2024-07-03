using IqraCore.Entities.Helper.Agent;

namespace IqraCore.Entities.Conversation
{
    public class ConversationConversationMessageResponseExecuteToolSystem : ConversationConversationMessageResponseExecuteTool
    {
        public AgentExecuteSystemToolTypeENUM ToolType { get; set; }
    }
}

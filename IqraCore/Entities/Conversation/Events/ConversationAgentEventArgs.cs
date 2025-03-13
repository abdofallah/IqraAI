using IqraCore.Entities.Conversation.Enum;
using IqraCore.Interfaces.Conversation;

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAgentThinkingEventArgs : EventArgs
    {
        public string ThoughtProcess { get; }
        public DateTime Timestamp { get; }

        public ConversationAgentThinkingEventArgs(string thoughtProcess)
        {
            ThoughtProcess = thoughtProcess;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationAgentErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public ConversationErrorSeverity Severity { get; }
        public DateTime Timestamp { get; }

        public ConversationAgentErrorEventArgs(string errorMessage, Exception exception = null, ConversationErrorSeverity severity = ConversationErrorSeverity.Error)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            Severity = severity;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class ConversationAgentAddedEventArgs : EventArgs
    {
        public IConversationAgent Agent { get; }

        public ConversationAgentAddedEventArgs(IConversationAgent agent)
        {
            Agent = agent;
        }
    }

    public class ConversationAgentRemovedEventArgs : EventArgs
    {
        public string AgentId { get; }
        public string Reason { get; }

        public ConversationAgentRemovedEventArgs(string agentId, string reason)
        {
            AgentId = agentId;
            Reason = reason;
        }
    }
}

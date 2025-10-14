using System.Net;

namespace IqraCore.Entities.Conversation.Events
{
    public class ConversationAgentEventLLMStreamCancelled
    {
        public ConversationAgentEventLLMStreamCancelledTypeEnum Type { get; set; }

        // For HttpRequestNotSuccess
        public HttpStatusCode? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }

        // For InternalExceptionError
        public Exception? Exception { get; set; }
    }

    public enum ConversationAgentEventLLMStreamCancelledTypeEnum
    {
        HttpRequestNotSuccess,
        InternalExceptionError,
        OperationCancelled
    }
}

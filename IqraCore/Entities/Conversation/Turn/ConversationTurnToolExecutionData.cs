using System.Text.Json;

namespace IqraCore.Entities.Conversation.Turn
{
    public class ConversationTurnToolExecutionData
    {
        public ConversationTurnAgentToolType ToolType { get; set; } // System or Custom
        public string? NodeId { get; set; } // The script node ID that defines this tool
        public string? ReasonForExecution { get; set; } // The reason provided by the LLM
        public string ToolName { get; set; }

        public string RawLLMInput { get; set; }
        public string? Result { get; set; }

        public DateTime? CompletedAt { get; set; }
        public bool WasSuccessful { get; set; }
    }
}
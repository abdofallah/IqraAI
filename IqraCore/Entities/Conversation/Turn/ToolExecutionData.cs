using System.Text.Json;

namespace IqraCore.Entities.Conversation.Turn
{
    public class ToolExecutionData
    {
        public AgentToolType ToolType { get; set; } // System or Custom
        public string NodeId { get; set; } // The script node ID that defines this tool
        public string ReasonForExecution { get; set; } // The reason provided by the LLM
        public string ToolName { get; set; }

        /// <summary>
        /// The full, raw command string from the LLM. Useful for debugging.
        /// </summary>
        public string RawLLMInput { get; set; }

        /// <summary>
        /// The parsed arguments sent to the tool. Stored as a flexible JSON object.
        /// </summary>
        public JsonDocument? ParsedArguments { get; set; }

        /// <summary>
        /// The raw string result from the tool's execution.
        /// </summary>
        public string? Result { get; set; }

        public DateTime? CompletedAt { get; set; }
        public bool WasSuccessful { get; set; }
    }
}
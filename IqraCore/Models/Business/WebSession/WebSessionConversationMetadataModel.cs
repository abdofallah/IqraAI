using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.WebSession;

namespace IqraCore.Models.Business.WebSession
{
    public class WebSessionConversationMetadataModel
    {
        public string QueueId { get; set; }
        public WebSessionStatusEnum Status { get; set; }

        // Queue Related
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessingStartedAt { get; set; } = null;
        public DateTime? CompletedAt { get; set; } = null;

        // General / Web Session Specific
        public string ClientIdentifier { get; set; } = string.Empty;
        public string WebCampaignId { get; set; } = string.Empty;

        // Extra info often useful for the "Manage View"
        public Dictionary<string, string> DynamicVariables { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();

        // Queue Session Related (Enriched from ConversationState)
        public string? SessionId { get; set; } = null;
        public ConversationSessionState? SessionStatus { get; set; } = null;
        public ConversationSessionEndType? SessionEndType { get; set; } = null;
    }
}

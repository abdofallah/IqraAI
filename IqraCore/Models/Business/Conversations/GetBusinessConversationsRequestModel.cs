using IqraCore.Entities.Conversation.Enum;
using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.Business.Conversations
{
    public class GetBusinessConversationsRequestModel
    {
        [Range(1, 100)]
        public int Limit { get; set; } = 10;

        public string? NextCursor { get; set; } = null;
        public string? PreviousCursor { get; set; } = null;

        public GetBusinessConversationsRequestFilterModel? Filter { get; set; } = null;
    }

    public class GetBusinessConversationsRequestFilterModel
    {
        public DateTime? StartStartedDate { get; set; } = null;
        public DateTime? EndStartedDate { get; set; } = null;

        public List<ConversationSessionState>? SessionStates { get; set; } = null;
        public List<ConversationSessionInitiationType>? SessionInitiationTypes { get; set; } = null;
        public List<ConversationSessionEndType>? SessionEndTypes { get; set; } = null;
    }
}

using IqraCore.Entities.Helper;

namespace IqraCore.Entities.Conversation
{
    public class ConversationNumberActionTool
    {
        public long? SelectedToolId { get; set; }
        public Dictionary<long, (string, string)>? ArgumentResults { get; set; }
        public HttpStatusEnum? ResultStatus { get; set; }
    }
}

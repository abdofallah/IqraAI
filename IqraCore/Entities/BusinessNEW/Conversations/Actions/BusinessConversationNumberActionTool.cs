using IqraCore.Entities.Helper;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationNumberActionTool
    {
        public long? SelectedToolId { get; set; }
        public Dictionary<long, (string, string)>? ArgumentResults { get; set; }
        public HttpStatusEnum? ResultStatus { get; set; }
    }
}

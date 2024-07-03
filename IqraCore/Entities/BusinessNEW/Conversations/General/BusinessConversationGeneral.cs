using IqraCore.Entities.Helper.Session;

namespace IqraCore.Entities.BusinessNEW
{
    public class BusinessConversationGeneral
    {
        public DateTime Created { get; set; }
        public TimeSpan Timespan { get; set; }
        public bool EndedByUser { get; set; }
    }
}

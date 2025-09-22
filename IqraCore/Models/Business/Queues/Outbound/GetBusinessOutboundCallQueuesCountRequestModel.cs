using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Business.Queues.Outbound
{
    public class GetBusinessOutboundCallQueuesCountRequestModel
    {
        public DateTime? StartCreatedDate { get; set; } = null;
        public DateTime? EndCreatedDate { get; set; } = null;

        public DateTime? StartCompletedAtDate { get; set; } = null;
        public DateTime? EndCompletedAtDate { get; set; } = null;

        public DateTime? StartScheduledDate { get; set; } = null;
        public DateTime? EndScheduledDate { get; set; } = null;

        public List<CallQueueStatusEnum>? QueueStatusTypes { get; set; } = null;
        public List<string>? CampaignIds { get; set; } = null;
        public List<string>? CallingNumberIds { get; set; } = null;
        public List<TelephonyProviderEnum>? CallingNumberProviders { get; set; } = null;
        public List<string>? RecipientNumbers { get; set; } = null;
    }
}

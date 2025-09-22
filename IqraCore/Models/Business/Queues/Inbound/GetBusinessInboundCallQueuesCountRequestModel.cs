using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;

namespace IqraCore.Models.Business.Queues.Inbound
{
    public class GetBusinessInboundCallQueuesCountRequestModel
    {
        public DateTime? StartCreatedDate { get; set; } = null;
        public DateTime? EndCreatedDate { get; set; } = null;

        public DateTime? StartCompletedAtDate { get; set; } = null;
        public DateTime? EndCompletedAtDate { get; set; } = null;

        public List<CallQueueStatusEnum>? QueueStatusTypes { get; set; } = null;
        public List<string>? RouteIds { get; set; } = null;
        public List<string>? CallingNumbers { get; set; } = null;
        public List<TelephonyProviderEnum>? RouteNumberProviders { get; set; } = null;
        public List<string>? RouteNumberIds { get; set; } = null;
    }
}

using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.Business.Queues.Inbound
{
    public class GetBusinessInboundCallQueuesRequestModel
    {
        [Range(1, 100)]
        public int Limit { get; set; } = 10;

        public string? NextCursor { get; set; } = null;
        public string? PreviousCursor { get; set; } = null;

        public GetBusinessInboundCallQueuesRequestFilterModel? Filter { get; set; } = null;
    }

    public class GetBusinessInboundCallQueuesRequestFilterModel
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

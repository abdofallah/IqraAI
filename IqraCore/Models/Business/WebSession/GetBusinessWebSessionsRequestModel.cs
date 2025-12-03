using IqraCore.Entities.WebSession;
using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.Business.WebSession
{
    public class GetBusinessWebSessionsRequestModel
    {
        [Range(1, 100)]
        public int Limit { get; set; } = 10;

        public string? NextCursor { get; set; } = null;
        public string? PreviousCursor { get; set; } = null;

        public GetBusinessWebSessionsRequestFilterModel? Filter { get; set; } = null;
    }

    public class GetBusinessWebSessionsRequestFilterModel
    {
        // Date Ranges
        public DateTime? StartCreatedDate { get; set; } = null;
        public DateTime? EndCreatedDate { get; set; } = null;

        public DateTime? StartCompletedAtDate { get; set; } = null;
        public DateTime? EndCompletedAtDate { get; set; } = null;

        // Status Filtering
        public List<WebSessionStatusEnum>? QueueStatusTypes { get; set; } = null;

        // String Lists (for Tag Inputs)
        public List<string>? WebCampaignIds { get; set; } = null;
        public List<string>? ClientIdentifiers { get; set; } = null;
    }
}

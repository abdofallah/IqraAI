using System.ComponentModel.DataAnnotations;

namespace IqraCore.Models.User.Usage
{
    public class GetUserUsageHistoryRequestModel
    {
        [Range(1, 100)]
        public int Limit { get; set; } = 10;

        public string? NextCursor { get; set; }
        public string? PreviousCursor { get; set; }
        public List<long>? BusinessIds { get; set; } = null;
    }
}

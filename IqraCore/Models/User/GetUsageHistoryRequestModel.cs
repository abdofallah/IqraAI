namespace IqraCore.Models.User
{
    public class GetUsageHistoryRequestModel
    {
        public int Limit { get; set; } = 10;
        public string? NextCursor { get; set; }
        public string? PreviousCursor { get; set; }
    }
}

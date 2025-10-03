namespace IqraCore.Models.User.Usage
{
    public class GetUserUsageCountResponseModel
    {
        public Dictionary<string, long> CurrentCounts { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, long>? PreviousCounts { get; set; } = null;
    }
}

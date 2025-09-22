namespace IqraCore.Models.User.Usage
{
    public class GetUserUsageCountResponseModel
    {
        public long CurrentCount { get; set; }
        public long? PreviousCount { get; set; }
    }
}

namespace IqraCore.Models.Business.Conversations
{
    public class GetSessionLogsRequestModel
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? LevelFilter { get; set; }
    }
}

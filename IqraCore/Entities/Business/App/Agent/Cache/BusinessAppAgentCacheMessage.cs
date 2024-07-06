namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCacheMessage
    {
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool IsQueryCaseSensitive { get; set; } = false;
    }
}

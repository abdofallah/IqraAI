namespace IqraCore.Entities.Business
{
    public class BusinessAppAgentCacheMessage
    {
        public string Query { get; set; }
        public string Response { get; set; }
        public bool IsQueryCaseSensitive { get; set; }
    }
}

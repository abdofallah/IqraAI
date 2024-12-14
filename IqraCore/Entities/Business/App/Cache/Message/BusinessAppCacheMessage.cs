namespace IqraCore.Entities.Business
{
    public class BusinessAppCacheMessage
    {
        public string Id { get; set; }
        public string Query { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public bool IsQueryCaseSensitive { get; set; } = false;
    }
}

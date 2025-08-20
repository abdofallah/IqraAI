namespace IqraCore.Models.KnowledgeBase
{
    public class KnowledgeBaseSearchResultModel
    {
        public float Score { get; set; }
        public string Text { get; set; }
        public string DocumentId { get; set; }
        public string ChunkId { get; set; }
        public string? ParentChunkid { get; set; }
    }
}

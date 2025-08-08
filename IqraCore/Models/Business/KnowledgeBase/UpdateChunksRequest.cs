namespace IqraCore.Models.Business.KnowledgeBase
{
    public class UpdateChunksRequest
    {
        public List<AddedChunkInfo> Added { get; set; } = new();
        public List<EditedChunkInfo> Edited { get; set; } = new();
        public List<string> Deleted { get; set; } = new();
    }

    public class AddedChunkInfo
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string? ParentId { get; set; }
    }

    public class EditedChunkInfo
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
}

namespace IqraCore.Interfaces.AI
{
    public class EmbeddingResult
    {
        public float[]? Vector { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IEmbeddingService : IDisposable
    {
        Task<EmbeddingResult> GenerateEmbeddingAsync(string text, int? dimensions = null);
    }
}

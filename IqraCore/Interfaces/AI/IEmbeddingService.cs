using IqraCore.Entities.Helpers;

namespace IqraCore.Interfaces.AI
{
    public interface IEmbeddingService : IDisposable
    {
        Task<FunctionReturnResult<float[]?>> GenerateEmbeddingForTextAsync(string text);

        Task<FunctionReturnResult<List<float[]>?>> GenerateEmbeddingForTextListAsync(List<string> texts);
    }
}

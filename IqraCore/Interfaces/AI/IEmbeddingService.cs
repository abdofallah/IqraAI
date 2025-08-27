using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Interfaces.Embedding;

namespace IqraCore.Interfaces.AI
{
    public interface IEmbeddingService : IDisposable
    {
        Task<FunctionReturnResult<float[]?>> GenerateEmbeddingForTextAsync(string text);
        Task<FunctionReturnResult<List<float[]>?>> GenerateEmbeddingForTextListAsync(List<string> texts);

        InterfaceEmbeddingProviderEnum GetProviderType();
        static InterfaceEmbeddingProviderEnum GetProviderTypeStatic()
        {
            return InterfaceEmbeddingProviderEnum.Unknown;
        }

        IEmbeddingConfig GetCacheableConfig();
    }
}

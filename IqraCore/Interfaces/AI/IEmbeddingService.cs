using IqraCore.Entities.Business;

namespace IqraCore.Interfaces.AI
{
    public interface IEmbeddingService : IDisposable
    {
        Task GetEmbeddingAsync(string text, BusinessAppAgentIntegrationData embedding);
    }
}

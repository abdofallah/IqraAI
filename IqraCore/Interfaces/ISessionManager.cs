using System;
using System.Threading.Tasks;

namespace IqraCore.Interfaces
{
    public interface ISessionManager
    {
        Task<string> CreateSessionAsync();
        Task StoreConversationDataAsync(string sessionId, string conversationData);
        Task<string> GetConversationDataAsync(string sessionId);
    }
}
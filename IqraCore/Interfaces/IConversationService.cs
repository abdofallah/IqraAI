using System.Threading.Tasks;
using IqraCore.Entities;

namespace IqraCore.Interfaces
{
    public interface IConversationService
    {
        Task<Session> StartConversationAsync();
        Task<Conversation> ProcessUserInputAsync(string sessionId, string userInput);
        Task EndConversationAsync(string sessionId);
    }
}
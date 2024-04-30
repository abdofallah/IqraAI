using System.Threading.Tasks;
using IqraCore.Entities;
using IqraCore.Entities.Session.Conversation;

namespace IqraCore.Interfaces
{
    public interface IConversationService
    {
        Task<Session> StartConversationAsync();
        Task<SessionConversation> ProcessUserInputAsync(string sessionId, string userInput);
        Task EndConversationAsync(string sessionId);
    }
}
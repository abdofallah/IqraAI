using System.Collections.Generic;
using System.Threading.Tasks;
using IqraCore.Entities;

namespace IqraCore.Interfaces.Repositories
{
    public interface IConversationRepository
    {
        Task AddConversationAsync(Conversation conversation);
        Task<IEnumerable<Conversation>> GetConversationsBySessionAsync(string sessionId);
    }
}
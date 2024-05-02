using IqraCore.Entities;
using System.Threading.Tasks;

namespace IqraCore.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task<Session> CreateSessionAsync();
        Task<Session> GetSessionAsync(string sessionId);
        Task UpdateSessionAsync(Session session);
    }
}
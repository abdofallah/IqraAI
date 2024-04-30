using System.Threading.Tasks;
using IqraCore.Entities.Session;

namespace IqraCore.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task<Session> CreateSessionAsync();
        Task<Session> GetSessionAsync(string sessionId);
        Task UpdateSessionAsync(Session session);
    }
}
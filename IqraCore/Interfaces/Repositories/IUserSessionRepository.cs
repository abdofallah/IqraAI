namespace IqraCore.Interfaces.Repositories
{
    public interface IUserSessionRepository
    {
        Task<bool> CreateSession(string userId, string sessionId, string authenticationKey, int expireHours);
        Task<bool> RemoveSession(string userId, string sessionId);
        Task<string?> RetrieveSession(string userId, string sessionId);
        Task<bool> ValidateSession(string userId, string sessionId, string authenticationKey);
    }
}

using IqraCore.Entities.User;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<List<UserData>> GetUsersAsync();
        Task<List<UserData>> GetUsersAsync(int page, int pageSize);
        Task<bool> AddUserAsync(UserData user);
        Task<UserData?> GetUserByEmail(string email);
        Task<bool> UpdateUser(string email, UpdateDefinition<UserData> updateDefinition);
    }
}

using IqraCore.Entities.User;
using MongoDB.Driver;

namespace IqraCore.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<bool> AddUserAsync(User user);
        Task<User?> GetUserByEmail(string email);
        Task<bool> UpdateUser(string email, UpdateDefinition<User> updateDefinition);
    }
}

using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Authentication;
using MongoDB.Driver;

namespace IqraCore.Interfaces.User
{
    public interface IUserRegistrationManager
    {
        Task<FunctionReturnResult<UserData?>> RegisterUser(RegisterModel model, Func<string, string, string> hashPasswordFunction, string? isAdmin = null, IClientSessionHandle? mongoSession = null);
    }
}

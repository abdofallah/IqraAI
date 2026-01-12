using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Authentication;

namespace IqraCore.Interfaces.User
{
    public interface IUserRegistrationManager
    {
        Task<FunctionReturnResult<UserData?>> RegisterUser(RegisterModel model, Func<string, string, string> hashPasswordFunction, string? isAdmin = null);
    }
}

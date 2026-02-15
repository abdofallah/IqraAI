using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.User;

namespace IqraCore.Interfaces.User
{
    public interface IUserApiKeyManager
    {
        Task<UserData?> GetFullUserByEmailHash(string email);
        Task<FunctionReturnResult<UserApiKeyCreateModel?>> CreateUserApiKeyAsync(UserData user, string friendlyName, bool allowUserManagementApiRequests, List<long> restrictedBusinessIds);
        Task<FunctionReturnResult> DeleteUserApiKeyAsync(string userEmail, string userApiKeyId);
        string HashUserEmail(string userEmail);
        Task<FunctionReturnResult<(UserData? User, UserApiKey? ApiKey)>> ValidateUserApiKeyAsync(string rawApiKey);
    }
}
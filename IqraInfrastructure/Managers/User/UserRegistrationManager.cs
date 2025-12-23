using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Interfaces.User;
using IqraCore.Models.Authentication;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Repositories.User;

namespace IqraInfrastructure.Managers.User
{
    public class UserRegistrationManager : IUserRegistrationManager
    {
        private readonly UserApiKeyProcessor _apiKeyProcessor;
        private readonly UserRepository _userRepoistory;

        public UserRegistrationManager(
            UserApiKeyProcessor apiKeyProcessor,
            UserRepository userRepoistory
        )
        {
            _apiKeyProcessor = apiKeyProcessor;
            _userRepoistory = userRepoistory;
        }

        public async Task<FunctionReturnResult<UserData?>> RegisterUser(RegisterModel model, Func<string, string, string> hashPasswordFunction)
        {
            var result = new FunctionReturnResult<UserData?>();

            UserData newUser = new UserData
            {
                Email = model.Email,
                EmailHash = _apiKeyProcessor.ComputeEmailHash(model.Email),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordSHA = hashPasswordFunction(model.Email, model.Password)
            };

            var addResult = await _userRepoistory.AddUserAsync(newUser);
            if (!addResult)
            {
                return result.SetFailureResult(
                    "RegisterUser:USER_CREATION_FAILED",
                    "User creation failed in db."
                );
            }

            return result.SetSuccessResult(newUser);
        }
    }
}

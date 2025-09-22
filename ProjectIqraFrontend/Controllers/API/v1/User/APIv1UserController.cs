using IqraCore.Entities.Helpers;
using IqraCore.Models.User.GetMasterUserDataModel;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.API.v1.User
{
    [ApiController]
    [Route("api/v1/user")]
    public class APIv1UserController : Controller
    {
        private readonly UserAPIValidationHelper _userAPIValidationHelper;

        public APIv1UserController(UserAPIValidationHelper userAPIValidationHelper)
        {
            _userAPIValidationHelper = userAPIValidationHelper;
        }

        [HttpGet]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetUserData()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userAPIValidationHelper.ValidateUserAPIAsync(Request);
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversations:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }
                var userData = apiKeyValidaiton.Data!.userData!;

                GetMasterUserDataModel userDataModel = new GetMasterUserDataModel(userData);

                return result.SetSuccessResult(userDataModel);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUserData:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}

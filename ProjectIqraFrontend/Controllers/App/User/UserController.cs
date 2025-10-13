using IqraCore.Entities.Helpers;
using IqraCore.Models.User.GetMasterUserDataModel;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;

        public UserController(
            UserSessionValidationHelper userSessionValidationHelper
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
        }

        [HttpGet("/app/user")]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetMasterUserDataModel()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                var validationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled: true);
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetMasterUserDataModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!;

                GetMasterUserDataModel userDataModel = new GetMasterUserDataModel(userData);

                return result.SetSuccessResult(userDataModel);
            }
            catch ( Exception ex ) {
                return result.SetFailureResult(
                    "GetMasterUserDataModel:EXCEPTION",
                    $"Internal server error: {ex.Message}"
                );
            }
        }
    }
}

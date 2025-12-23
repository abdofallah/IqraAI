using IqraCore.Attributes;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.MasterUserDataModel;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.App.User
{
    public class UserController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;

        public UserController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
        }

        [OpenSourceOnly]
        [HttpGet("/app/user")]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetMasterUserDataModel()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetMasterUserDataModel:{validationResult.Code}",
                        validationResult.Message
                    );
                }
                var userData = validationResult.Data!.userData!;

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

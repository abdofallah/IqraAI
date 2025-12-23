using IqraCore.Attributes;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.User.MasterUserDataModel;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.API.v1.User
{
    [ApiController]
    [Route("api/v1/user")]
    public class APIv1UserController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;

        public APIv1UserController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
        }

        [OpenSourceOnly]
        [HttpGet]
        public async Task<FunctionReturnResult<GetMasterUserDataModel?>> GetUserData()
        {
            var result = new FunctionReturnResult<GetMasterUserDataModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIWithPermissions(
                    Request: Request,
                    checkUserDisabled: true
                );
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

using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminUsersController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppAdminUsersController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            UserManager userManager,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/admin/users")]
        public async Task<FunctionReturnResult<List<UserData>?>> GetUsers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<UserData>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUsers:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var usersResult = await _userManager.GetUsersAsync(page, pageSize);
                if (!usersResult.Success)
                {
                    return result.SetFailureResult(
                        "GetUsers:" + usersResult.Code,
                        usersResult.Message
                    );
                }

                return result.SetSuccessResult(usersResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUsers:EXCEPTION", 
                    $"Error getting users: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/user")]
        public async Task<FunctionReturnResult<UserData?>> GetUser(string email)
        {
            var result = new FunctionReturnResult<UserData?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUser:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var resultUser = await _userManager.GetFullUserByEmail(email);
                if (resultUser == null)
                {
                    return result.SetFailureResult(
                        "GetUser:USER_NOT_FOUND",
                        "User not found"
                    );
                }

                return result.SetSuccessResult(resultUser);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUser:EXCEPTION",
                    $"Error getting user: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/user/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinesses(string inputUserEmail, List<long> businessIds)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            try
            {
                var validationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionWithPermissions(
                    Request: Request,
                    checkUserIsAdmin: true,
                    checkUserDisabled: true
                );
                if (!validationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetUserBusinesses:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var businessesResult = await _businessManager.GetUserBusinessesByIds(businessIds, inputUserEmail);
                if (!businessesResult.Success)
                {
                    return result.SetFailureResult(
                        "GetUserBusinesses:" + businessesResult.Code,
                        businessesResult.Message
                    );
                }

                return result.SetSuccessResult(businessesResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetUserBusinesses:EXCEPTION",
                    $"Error getting user businesses: {ex.Message}"
                );
            }
        }
    }
}

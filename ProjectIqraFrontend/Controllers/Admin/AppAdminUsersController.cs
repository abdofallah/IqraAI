using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminUsersController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppAdminUsersController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/admin/users")]
        public async Task<FunctionReturnResult<List<UserData>?>> GetUsers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<UserData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUsers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetUsers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUsers:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUsers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var usersResult = await _userManager.GetUsersAsync(page, pageSize);
            if (!usersResult.Success)
            {
                result.Code = "GetUsers:" + usersResult.Code;
                result.Message = usersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = usersResult.Data;

            return result;
        }

        [HttpPost("/app/admin/user")]
        public async Task<FunctionReturnResult<UserData?>> GetUser(string email)
        {
            var result = new FunctionReturnResult<UserData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUser:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetUser:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUser:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUser:4";
                result.Message = "User is not an admin";
                return result;
            }

            var resultUser = await _userManager.GetFullUserByEmail(email);
            if (resultUser == null)
            {
                result.Code = "GetUser:5";
                result.Message = "User not found";
                return result;
            }

            result.Success = true;
            result.Data = resultUser;

            return result;
        }

        [HttpPost("/app/admin/user/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinesses(string inputUserEmail, List<long> businessIds)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetUserBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUserBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetUserBusinessesByIds(businessIds, inputUserEmail);
            if (!businessesResult.Success)
            {
                result.Code = "GetUserBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        } 

    }
}

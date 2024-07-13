using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraCore.Entities.Region;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppAdminController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;

        public AppAdminController(UserManager userManager, BusinessManager businessManager, RegionManager regionManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _regionManager = regionManager;
        }

        /**
         * 
         * Users
         * 
        **/ 

        [HttpPost("/app/admin/users")]
        public async Task<FunctionReturnResult<List<UserData>?>> GetUsers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<UserData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            var usersResult = await _userManager.GetUsersAsync(page, pageSize);
            if (!usersResult.Success)
            {
                result.Code = 1000 + usersResult.Code;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            var resultUser = await _userManager.GetUserByEmail(email);
            if (resultUser == null)
            {
                result.Code = 5;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetUserBusinessesByIds(businessIds, inputUserEmail);
            if (!businessesResult.Success)
            {
                result.Code = 1000 + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/admin/user/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(string inputUserEmail, List<long> numberIds)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            result.Success = true;
            result.Data = new List<NumberData>();
            return result;

            /**
            var numbersResult = await _numberManager.GetUserNumbersByIds(numberIds, inputUserEmail);
            if (!numbersResult.Success)
            {
                result.Code = 1000 + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
            **/
        }

        /**
         * 
         * Businesses
         * 
        **/

        [HttpPost("/app/admin/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetBusinesses(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetBusinesses(page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = 1000 + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/admin/business/search")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> SearchBusinesses(string query, int pageSize = 10, int page = 0)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = 5;
                result.Message = "Query cannot be empty";
                return result;
            }

            var businessesResult = await _businessManager.SearchBusinesses(query, page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = 1000 + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/admin/business/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetBusinessNumbers(long businessId, List<long> numberIds)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            result.Success = true;
            result.Data = new List<NumberData>();
            return result;

            /**
            var numbersResult = await _numberManager.GetBusinessNumbersByIds(numberIds, inputUserEmail);
            if (!numbersResult.Success)
            {
                result.Code = 1000 + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
            **/
        }

        [HttpPost("/app/admin/regions")]
        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<RegionData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = 4;
                result.Message = "User is not an admin";
                return result;
            }

            var regionsResult = await _regionManager.GetRegions(page, pageSize);
            if (!regionsResult.Success)
            {
                result.Code = 1000 + regionsResult.Code;
                result.Message = regionsResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = regionsResult.Data;

            return result;
        }
    }
}

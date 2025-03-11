using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminBusinessesController : Controller
    {
        private readonly BusinessManager _businessManager;
        private readonly UserManager _userManager;

        public AppAdminBusinessesController(BusinessManager businessManager, UserManager userManager)
        {
            _businessManager = businessManager;
            _userManager = userManager;
        }

        [HttpPost("/app/admin/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetBusinesses(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetBusinesses(page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = "GetBusinesses:" + businessesResult.Code;
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
                result.Code = "SearchBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SearchBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SearchBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SearchBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = "SearchBusinesses:5";
                result.Message = "Query cannot be empty";
                return result;
            }

            var businessesResult = await _businessManager.SearchBusinesses(query, page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = "SearchBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }
    }
}

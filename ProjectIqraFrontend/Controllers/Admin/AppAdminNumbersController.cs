using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminNumbersController : Controller
    {
        private readonly NumberManager _numberManager;
        private readonly UserManager _userManager;

        public AppAdminNumbersController(NumberManager numberManager, UserManager userManager)
        {
            _numberManager = numberManager;
            _userManager = userManager;
        }

        [HttpPost("/app/admin/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetNumbers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetNumbers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetNumbers:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetNumbers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var numbersResult = await _numberManager.GetNumbers(page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = "GetNumbers:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

        [HttpPost("/app/admin/numbers/{provider}")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbersByType(int provider, int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetNumbersByType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetNumbersByType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetNumbersByType:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetNumbersByType:4";
                result.Message = "User is not an admin";
                return result;
            }

            if (!Enum.IsDefined(typeof(NumberProviderEnum), provider))
            {
                result.Code = "GetNumbersByType:5";
                result.Message = "Invalid provider";
                return result;
            }

            var numbersResult = await _numberManager.GetNumbersByProvider((NumberProviderEnum)provider, page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = "GetNumbersByType:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

    }
}

using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessNumbersController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly NumberManager _numberManager;
        
        public AppUserBusinessNumbersController(UserManager userManager, BusinessManager businessManager, NumberManager numberManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _numberManager = numberManager;
        }

        [HttpPost("/app/user/business/{businessId}/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetBusinessNumbers(long businessId)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();
            
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];
            
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinessNumbers:1";
                result.Message = "Invalid session data";
                return result;
            }
            
            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetBusinessNumbers:2";
                result.Message = "Session validation failed";
                return result;
            }
            
            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinessNumbers:3";
                result.Message = "User not found";
                return result;
            }
            
            var businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "GetBusinessNumbers:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }
            
            var numbersResult = await _numberManager.GetBusinessNumberByIds(businessResult.Data.NumberIds, businessId);
            if (!numbersResult.Success)
            {
                result.Code = "GetBusinessNumbers:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }
            
            result.Data = numbersResult.Data;
            return result;
        }
    }
}

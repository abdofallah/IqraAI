using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Languages;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppSpecificationController : Controller
    {
        private readonly UserManager _userManager;
        private readonly LanguagesManager _languagesManager;

        public AppSpecificationController(UserManager userManager, LanguagesManager languagesManager)
        {
            _userManager = userManager;
            _languagesManager = languagesManager;
        }

        [HttpPost("/app/specification/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetAppLanguages([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetAppLanguages:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetAppLanguages:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetAppLanguages:3";
                result.Message = "User not found";
                return result;
            }

            var getLanguagesListResult = await _languagesManager.GetAllLanguagesList();
            if (!getLanguagesListResult.Success)
            {
                result.Code = "GetAppLanguages:" + getLanguagesListResult.Code;
                result.Message = getLanguagesListResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = getLanguagesListResult.Data;
            return result;
        }
    }
}

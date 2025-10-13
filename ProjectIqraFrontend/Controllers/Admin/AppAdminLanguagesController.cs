using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Languages;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminLanguagesController : Controller
    {
        private readonly LanguagesManager _languagesManager;
        private readonly UserManager _userManager;

        public AppAdminLanguagesController(LanguagesManager languagesManager, UserManager userManager)
        {
            _languagesManager = languagesManager;
            _userManager = userManager;
        }

        [HttpPost("/app/admin/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguages(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetLanguages:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetLanguages:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetLanguages:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetLanguages:4";
                result.Message = "User is not an admin";
                return result;
            }

            var languagesResult = await _languagesManager.GetLanguagesList(page, pageSize);
            if (!languagesResult.Success)
            {
                result.Code = "GetLanguages:" + languagesResult.Code;
                result.Message = languagesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = languagesResult.Data;
            return result;
        }

        [HttpPost("/app/admin/languages/save")]
        public async Task<FunctionReturnResult<LanguagesData?>> SaveLanguage(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLanguage:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLanguage:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLanguage:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLanguage:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveLanguage:5";
                result.Message = "postType is required or is invalid";
                return result;
            }

            string? languageCode = formData["languageCode"];
            if (string.IsNullOrEmpty(languageCode))
            {
                result.Code = "SaveLanguage:6";
                result.Message = "languageCode is required";
                return result;
            }

            var languagesDataResult = await _languagesManager.GetLanguageByCode(languageCode);

            if (postType == "new")
            {
                if (languagesDataResult.Data != null)
                {
                    result.Code = "SaveLanguage:7";
                    result.Message = "Language already exists";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (languagesDataResult.Data == null)
                {
                    result.Code = "SaveLanguage:8";
                    result.Message = "Language not found";
                    return result;
                }
            }

            var languageAddUpdateResult = await _languagesManager.AddUpdateLanguage(formData, postType, languageCode, languagesDataResult.Data);
            if (!languageAddUpdateResult.Success)
            {
                result.Code = "SaveLanguage:" + languageAddUpdateResult.Code;
                result.Message = languageAddUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = languageAddUpdateResult.Data;
            return result;
        }

    }
}

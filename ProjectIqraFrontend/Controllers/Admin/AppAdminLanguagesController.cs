using IqraCore.Entities.Helpers;
using IqraCore.Entities.Languages;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Languages;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminLanguagesController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly LanguagesManager _languagesManager;

        public AppAdminLanguagesController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            LanguagesManager languagesManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _languagesManager = languagesManager;
        }

        [HttpPost("/app/admin/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguages(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

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
                        $"GetLanguages:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                var languagesResult = await _languagesManager.GetLanguagesList(page, pageSize);
                if (!languagesResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetLanguages:{languagesResult.Code}",
                        languagesResult.Message
                    );
                }

                return result.SetSuccessResult(languagesResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetLanguages:EXCEPTION",
                    $"Failed to get languages. Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/admin/languages/save")]
        public async Task<FunctionReturnResult<LanguagesData?>> SaveLanguage(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

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
                        $"SaveLanguage:{validationResult.Code}",
                        validationResult.Message
                    );
                }

                string? postType = formData["postType"];
                if (string.IsNullOrEmpty(postType) || (postType != "new" && postType != "edit"))
                {
                    return result.SetFailureResult(
                        "SaveLanguage:INVALID_POST_TYPE",
                        "Invalid post type"
                    );
                }

                string? languageCode = formData["languageCode"];
                if (string.IsNullOrEmpty(languageCode))
                {
                    return result.SetFailureResult(
                        "SaveLanguage:EMPTY_LANGUAGE_CODE",
                        "Language code is required"
                    );
                }

                var languagesDataResult = await _languagesManager.GetLanguageByCode(languageCode);

                if (postType == "new")
                {
                    if (languagesDataResult.Data != null)
                    {
                        return result.SetFailureResult(
                            "SaveLanguage:ALREADY_EXISTS",
                            "Language already exists with id"
                        );
                    }
                }
                else if (postType == "edit")
                {
                    if (languagesDataResult.Data == null)
                    {
                        return result.SetFailureResult(
                            "SaveLanguage:NOT_FOUND",
                            "Language not found"
                        );
                    }
                }

                var languageAddUpdateResult = await _languagesManager.AddUpdateLanguage(formData, postType, languageCode, languagesDataResult.Data);
                if (!languageAddUpdateResult.Success)
                {
                    return result.SetFailureResult(
                        $"SaveLanguage:{languageAddUpdateResult.Code}",
                        languageAddUpdateResult.Message
                    );
                }

                return result.SetSuccessResult(languageAddUpdateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveLanguage:EXCEPTION",
                    $"Failed to save language. Exception: {ex.Message}"
                );
            }
        }
    }
}

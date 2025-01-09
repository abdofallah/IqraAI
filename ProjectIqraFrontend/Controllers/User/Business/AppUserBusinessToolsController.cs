using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessToolsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessToolsController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/tools/save")]
        public async Task<FunctionReturnResult<BusinessAppTool?>> SaveBusinessTools(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppTool?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessTools:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessTools:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessTools:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessTools:4";
                result.Message = "User does not have permission to edit businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessTools:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessTools:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessTools:6";
                result.Message = "Business is currently disabled";

                if (businessResult.Data.Permission.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Tools.DisabledFullAt != null || businessResult.Data.Permission.Tools.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessTools:7";
                result.Message = "Business does not have permission to edit tools";

                if (businessResult.Data.Permission.Tools.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Tools.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Tools.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Tools.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Tools.DisabledEditingReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType)
                ||
                postType != "new" && postType != "edit"
            )
            {
                result.Code = "SaveBusinessTools:7";
                result.Message = "Invalid post type.";
                return result;
            }

            formData.TryGetValue("exisitingToolId", out StringValues exisitingToolIdStringValue);
            string? exisitingToolIdValue = exisitingToolIdStringValue.ToString();
            if (postType == "edit")
            {
                if (string.IsNullOrWhiteSpace(exisitingToolIdValue))
                {
                    result.Code = "SaveBusinessTools:8";
                    result.Message = "Missing exisiting tool id.";
                    return result;
                }

                bool toolExistsResult = await _businessManager.GetToolsManager().CheckBusinessToolExists(businessId, exisitingToolIdValue);
                if (!toolExistsResult)
                {
                    result.Code = "SaveBusinessTools:9";
                    result.Message = "Exisiting tool not found.";
                    return result;
                }
            }

            FunctionReturnResult<BusinessAppTool?> updateResult = await _businessManager.GetToolsManager().AddOrUpdateUserBusinessTools(businessId, formData, postType, exisitingToolIdValue);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessTools:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }
    }
}

using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers
{
    public class AppUserBusinessContextController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessContextController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/context/branding/save")]
        public async Task<FunctionReturnResult<BusinessAppContextBranding?>> SaveBusinessContextBranding(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranding?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessContextBranding:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessContextBranding:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessContextBranding:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessContextBranding:4";
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
                result.Code = "SaveBusinessContextBranding:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessContextBranding:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessContextBranding:6";
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

            if (businessResult.Data.Permission.Context.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessContextBranding:7";
                result.Message = "Business does not have permission to edit context";

                if (businessResult.Data.Permission.Context.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Context.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Context.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Context.Branding.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessContextBranding:8";
                result.Message = "Business does not have permission to edit context branding";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Branding.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Context.Branding.DisabledEditingReason;
                }

                return result;
            }

            FunctionReturnResult<BusinessAppContextBranding?> updateResult = await _businessManager.UpdateUserBusinessContextBranding(businessId, formData);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessContextBranding:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/context/branches/save")]
        public async Task<FunctionReturnResult<BusinessAppContextBranch?>> SaveBusinessContextBranch(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextBranch?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessContextBranch:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessContextBranch:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessContextBranch:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessContextBranch:4";
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
                result.Code = "SaveBusinessContextBranch:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessContextBranch:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessContextBranch:6";
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

            if (businessResult.Data.Permission.Context.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessContextBranch:7";
                result.Message = "Business does not have permission to edit context";

                if (businessResult.Data.Permission.Context.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Context.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Context.DisabledFullReason;
                }

                return result;
            }         

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType)
                ||
                (postType != "new" && postType != "edit")
            )
            {
                result.Code = "SaveBusinessContextBranch:9";
                result.Message = "Invalid post type.";
                return result;
            }

            if (postType == "new")
            {
                if (businessResult.Data.Permission.Context.Branches.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessContextBranch:8";
                    result.Message = "Business does not have permission to add context branding";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Branches.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Branches.DisabledAddingReason;
                    }

                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (businessResult.Data.Permission.Context.Branches.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessContextBranch:8";
                    result.Message = "Business does not have permission to edit context branding";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Branches.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Branches.DisabledEditingReason;
                    }

                    return result;
                }
            }

            formData.TryGetValue("exisitingBranchId", out StringValues exisitingToolIdStringValue);
            string? exisitingBranchIdValue = exisitingToolIdStringValue.ToString();
            if (postType == "edit")
            {
                if (string.IsNullOrWhiteSpace(exisitingBranchIdValue))
                {
                    result.Code = "SaveBusinessContextBranch:8";
                    result.Message = "Missing exisiting branch id.";
                    return result;
                }

                bool branchExistsResult = await _businessManager.CheckBusinessBranchExists(businessId, exisitingBranchIdValue);
                if (!branchExistsResult)
                {
                    result.Code = "SaveBusinessContextBranch:9";
                    result.Message = "Exisiting branch not found.";
                    return result;
                }
            }

            FunctionReturnResult<BusinessAppContextBranch?> updateResult = await _businessManager.AddOrUpdateUserBusinessContextBranch(businessId, formData, postType, exisitingBranchIdValue);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessContextBranch:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }
    }
}

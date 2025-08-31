using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class UserBusinessContextController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public UserBusinessContextController(UserManager userManager, BusinessManager businessManager)
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

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
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

            FunctionReturnResult<BusinessAppContextBranding?> updateResult = await _businessManager.GetContextManager().UpdateUserBusinessContextBranding(businessId, formData);
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

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
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
                postType != "new" && postType != "edit"
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

                bool branchExistsResult = await _businessManager.GetContextManager().CheckBusinessBranchExists(businessId, exisitingBranchIdValue);
                if (!branchExistsResult)
                {
                    result.Code = "SaveBusinessContextBranch:9";
                    result.Message = "Exisiting branch not found.";
                    return result;
                }
            }

            FunctionReturnResult<BusinessAppContextBranch?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextBranch(businessId, formData, postType, exisitingBranchIdValue);
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

        [HttpPost("/app/user/business/{businessId}/context/services/save")]
        public async Task<FunctionReturnResult<BusinessAppContextService?>> SaveBusinessContextService(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextService?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessContextService:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessContextService:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessContextService:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessContextService:4";
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
                result.Code = "SaveBusinessContextService:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessContextService:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessContextService:6";
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
                result.Code = "SaveBusinessContextService:7";
                result.Message = "Business does not have permission to edit context";

                if (businessResult.Data.Permission.Context.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Context.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Context.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType) ||
                postType != "new" && postType != "edit"
            )
            {
                result.Code = "SaveBusinessContextService:8";
                result.Message = "Invalid post type.";
                return result;
            }

            if (postType == "new")
            {
                if (businessResult.Data.Permission.Context.Services.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessContextService:9";
                    result.Message = "Business does not have permission to add services";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Services.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Services.DisabledAddingReason;
                    }

                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (businessResult.Data.Permission.Context.Services.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessContextService:10";
                    result.Message = "Business does not have permission to edit services";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Services.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Services.DisabledEditingReason;
                    }

                    return result;
                }
            }

            formData.TryGetValue("exisitingServiceId", out StringValues exisitingServiceIdStringValue);
            string? exisitingServiceId = exisitingServiceIdStringValue.ToString();

            if (postType == "edit")
            {
                if (string.IsNullOrWhiteSpace(exisitingServiceId))
                {
                    result.Code = "SaveBusinessContextService:11";
                    result.Message = "Missing existing service id.";
                    return result;
                }

                bool serviceExistsResult = await _businessManager.GetContextManager().CheckBusinessServiceExists(businessId, exisitingServiceId);
                if (!serviceExistsResult)
                {
                    result.Code = "SaveBusinessContextService:12";
                    result.Message = "Existing service not found.";
                    return result;
                }
            }

            FunctionReturnResult<BusinessAppContextService?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextService(
                businessId,
                formData,
                postType,
                exisitingServiceId
            );

            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessContextService:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/context/products/save")]
        public async Task<FunctionReturnResult<BusinessAppContextProduct?>> SaveBusinessContextProduct(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppContextProduct?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessContextProduct:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessContextProduct:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessContextProduct:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessContextProduct:4";
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
                result.Code = "SaveBusinessContextProduct:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessContextProduct:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessContextProduct:6";
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
                result.Code = "SaveBusinessContextProduct:7";
                result.Message = "Business does not have permission to edit context";

                if (businessResult.Data.Permission.Context.DisabledFullAt != null && !string.IsNullOrEmpty(businessResult.Data.Permission.Context.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Context.DisabledFullReason;
                }

                return result;
            }

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType) ||
                postType != "new" && postType != "edit"
            )
            {
                result.Code = "SaveBusinessContextProduct:8";
                result.Message = "Invalid post type.";
                return result;
            }

            if (postType == "new")
            {
                if (businessResult.Data.Permission.Context.Products.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessContextProduct:9";
                    result.Message = "Business does not have permission to add products";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Products.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Products.DisabledAddingReason;
                    }

                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (businessResult.Data.Permission.Context.Products.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessContextProduct:10";
                    result.Message = "Business does not have permission to edit products";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Context.Products.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Context.Products.DisabledEditingReason;
                    }

                    return result;
                }
            }

            formData.TryGetValue("exisitingProductId", out StringValues exisitingProductIdStringValue);
            string? exisitingProductId = exisitingProductIdStringValue.ToString();

            if (postType == "edit")
            {
                if (string.IsNullOrWhiteSpace(exisitingProductId))
                {
                    result.Code = "SaveBusinessContextProduct:11";
                    result.Message = "Missing existing product id.";
                    return result;
                }

                bool productExistsResult = await _businessManager.GetContextManager().CheckBusinessProductExists(businessId, exisitingProductId);
                if (!productExistsResult)
                {
                    result.Code = "SaveBusinessContextProduct:12";
                    result.Message = "Existing product not found.";
                    return result;
                }
            }

            FunctionReturnResult<BusinessAppContextProduct?> updateResult = await _businessManager.GetContextManager().AddOrUpdateUserBusinessContextProduct(
                businessId,
                formData,
                postType,
                exisitingProductId
            );

            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessContextProduct:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }
    }
}

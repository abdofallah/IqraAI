using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessRoutingsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessRoutingsController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/routes/save")]
        public async Task<FunctionReturnResult<BusinessAppRoute?>> SaveBusinessRoute(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppRoute?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessRoute:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessRoute:2";
                result.Message = "Session validation failed";
                return result;
            }

            // Get and validate user
            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessRoute:3";
                result.Message = "User not found";
                return result;
            }

            // Check user permissions
            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessRoute:4";
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

            // Validate business ownership
            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessRoute:5";
                result.Message = "User does not own this business.";
                return result;
            }

            // Get and validate business
            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessRoute:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            // Check business permissions
            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessRoute:6";
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

            // Validate post type
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveBusinessRoute:7";
                result.Message = "Invalid post type.";
                return result;
            }

            // Validate existing route for edit
            formData.TryGetValue("existingRouteId", out StringValues existingRouteIdStringValue);
            string? existingRouteId = existingRouteIdStringValue.ToString();
            
            BusinessAppRoute? existingRouteData = null;
            if (postType == "new")
            {
                if (businessResult.Data.Permission.Routings.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessRoute:8";
                    result.Message = "Business does not have permission to add new routes";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Routings.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Routings.DisabledAddingReason;
                    }

                    return result;
                }
            }
            else
            {
                if (businessResult.Data.Permission.Routings.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessRoute:9";
                    result.Message = "Business does not have permission to edit routes";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Routings.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Routings.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(existingRouteId))
                {
                    result.Code = "SaveBusinessRoute:10";
                    result.Message = "Missing existing route id.";
                    return result;
                }

                existingRouteData = await _businessManager.GetRoutesManager().GetBusinessRoute(businessId, existingRouteId);
                if (existingRouteData == null)
                {
                    result.Code = "SaveBusinessRoute:11";
                    result.Message = "Existing route not found.";
                    return result;
                }
            }

            // Process the save/update
            FunctionReturnResult<BusinessAppRoute?> updateResult = await _businessManager.GetRoutesManager().AddOrUpdateUserBusinessRoute(businessId, formData, postType, existingRouteData);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessRoute:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Data = updateResult.Data;
            result.Success = true;
            return result;
        }
    }
}

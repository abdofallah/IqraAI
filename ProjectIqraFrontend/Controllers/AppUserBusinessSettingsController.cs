using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppUserBusinessSettingsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly NumberManager _numberManager;

        public AppUserBusinessSettingsController(UserManager userManager, BusinessManager businessManager, NumberManager numberManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _numberManager = numberManager;
        }

        [HttpPost("/app/user/business/{businessId}/settings/save")]
        public async Task<FunctionReturnResult<bool?>> SaveBusinessSettings(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<bool?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessSettings:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessSettings:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessSettings:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessSettings:4";
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
                result.Code = "SaveBusinessSettings:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessSettings:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessSettings:6";
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

            FunctionReturnResult<bool?> updateResult = await _businessManager.UpdateUserBusinessSettings(businessId, formData);
            if (!updateResult.Success)
            {
                result.Code = "SaveBusinessSettings:" + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}/domain/save")]
        public async Task<FunctionReturnResult<BusinessWhiteLabelDomain?>> SaveBusinessDomain(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessWhiteLabelDomain?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessDomain:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessDomain:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessDomain:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessDomain:4";
                result.Message = "User does not have permission to edit businesses";
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessDomain:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessDomain:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessDomain:6";
                result.Message = "Business does not have permission to edit settings";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType)
                ||
                (postType != "new" && postType != "edit")
            )
            {
                result.Code = "SaveBusinessDomain:7";
                result.Message = "Invalid post type.";
                return result;
            }

            BusinessWhiteLabelDomain? businessWhiteLabelDomainData = null;
            if (postType == "edit")
            {
                string domainIdString = formData["domainId"].ToString();
                if (string.IsNullOrWhiteSpace(domainIdString))
                {
                    result.Code = "SaveBusinessDomain:8";
                    result.Message = "Invalid domain id.";
                    return result;
                }

                if (!long.TryParse(domainIdString, out long domainId))
                {
                    result.Code = "SaveBusinessDomain:9";
                    result.Message = "Invalid domain id.";
                    return result;
                }

                if (!businessResult.Data.WhiteLabelDomainIds.Contains(domainId))
                {
                    result.Code = "SaveBusinessDomain:10";
                    result.Message = "The business does not own this domain.";
                    return result;
                }

                var domainResult = await _businessManager.GetUserBusinessWhiteLabelDomain(domainId, businessId, userEmail);
                if (!domainResult.Success)
                {
                    result.Code = "SaveBusinessDomain:" + domainResult.Code;
                    result.Message = domainResult.Message;
                    return result;
                }

                businessWhiteLabelDomainData = domainResult.Data;
            }

            FunctionReturnResult<BusinessWhiteLabelDomain?> addOrUpdateResult = await _businessManager.AddOrUpdateUserBusinessDomain(businessId, formData, postType, businessWhiteLabelDomainData);
            if (!addOrUpdateResult.Success)
            {
                result.Code = "SaveBusinessDomain:" + addOrUpdateResult.Code;
                result.Message = addOrUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = addOrUpdateResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/subuser/save")]
        public async Task<FunctionReturnResult<BusinessUser?>> SaveBusinessSubUser(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessUser?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessSubUser:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessSubUser:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessSubUser:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessSubUser:4";
                result.Message = "User does not have permission to edit businesses";
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessSubUser:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessSubUser:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessSubUser:6";
                result.Message = "Business does not have permission to edit settings";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (
                string.IsNullOrWhiteSpace(postType)
                ||
                (postType != "new" && postType != "edit")
            )
            {
                result.Code = "SaveBusinessSubUser:7";
                result.Message = "Invalid post type.";
                return result;
            }

            string subuserEmailString = formData["subUserEmail"].ToString();
            if (string.IsNullOrWhiteSpace(subuserEmailString))
            {
                result.Code = "SaveBusinessSubUser:8";
                result.Message = "Invalid subuser email.";
                return result;
            }

            BusinessUser? businessUserData = businessResult.Data.SubUsers.Find((d) =>
            {
                return d.Email == subuserEmailString;
            });

            if (postType == "new")
            {
                if (businessUserData != null)
                {
                    result.Code = "SaveBusinessSubUser:9";
                    result.Message = "The business already owns this subuser email.";
                    return result;
                }
            }

            if (postType == "edit")
            {
                if (businessUserData == null)
                {
                    result.Code = "SaveBusinessSubUser:10";
                    result.Message = "The business does not own this subuser email.";
                    return result;
                }
            }     

            FunctionReturnResult<BusinessUser?> addOrUpdateResult = await _businessManager.AddOrUpdateUserBusinessSubUser(businessId, formData, postType, businessResult.Data.WhiteLabelDomainIds, businessUserData);
            if (!addOrUpdateResult.Success)
            {
                result.Code = "SaveBusinessSubUser:" + addOrUpdateResult.Code;
                result.Message = addOrUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = addOrUpdateResult.Data;
            return result;
        }
    }
}

using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PhoneNumbers;
using System.Text.Json;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessNumbersController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        
        public AppUserBusinessNumbersController(UserManager userManager, BusinessManager businessManager, RegionManager regionManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _regionManager = regionManager;
        }

        [HttpPost("/app/user/business/{businessId}/numbers/save")]
        public async Task<FunctionReturnResult<BusinessNumberData?>> SaveBusinessNumber([FromForm] IFormCollection formData, long businessId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessNumber:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "SaveBusinessNumber:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessNumber:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessNumber:4";
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
                result.Code = "SaveBusinessNumber:5";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessNumber:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessNumber:6";
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

            if (businessResult.Data.Permission.Numbers.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessNumber:7";
                result.Message = "Business does not have permission to access numbers";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Numbers.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Numbers.DisabledFullReason;
                }

                return result;
            }

            if (!formData.TryGetValue("postType", out StringValues postTypeValue))
            {
                result.Code = "SaveBusinessNumber:8";
                result.Message = "Missing post type";
                return result;
            }

            string? postType = postTypeValue.ToString();
            if (string.IsNullOrWhiteSpace(postType)
                || postType != "new" && postType != "edit")
            {
                result.Code = "SaveBusinessNumber:9";
                result.Message = "Invalid post type";
                return result;
            }

            // Number Changes Data
            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "SaveBusinessNumber:10";
                result.Message = "Changes not found in form data.";
                return result;
            }
            JsonDocument? changes;
            try
            {
                changes = JsonDocument.Parse(changesJsonString);
            }
            catch
            {
                result.Code = "SaveBusinessNumber:11";
                result.Message = "Unable to parse changes json string.";
                return result;
            }

            // Get country code
            if (!changes.RootElement.TryGetProperty("countryCode", out var countryCodeElement))
            {
                result.Code = "SaveBusinessNumber:12";
                result.Message = "Country code not found in changes.";
                return result;
            }
            string? countryCode = countryCodeElement.GetString();
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                result.Code = "SaveBusinessNumber:13";
                result.Message = "Country code cannot be empty.";
                return result;
            }

            // Get number
            if (!changes.RootElement.TryGetProperty("number", out var numberElement))
            {
                result.Code = "SaveBusinessNumber:14";
                result.Message = "Number not found in changes.";
                return result;
            }
            string? number = numberElement.GetString();
            if (string.IsNullOrWhiteSpace(number))
            {
                result.Code = "SaveBusinessNumber:15";
                result.Message = "Number cannot be empty.";
                return result;
            }

            // Validate Number based on number and country code
            PhoneNumber parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(number, countryCode);
            if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
            {
                result.Code = "SaveBusinessNumber:16";
                result.Message = "Number validation failed for specified country";
                return result;
            }

            // Provider Type
            BusinessNumberProviderEnum provider = BusinessNumberProviderEnum.Unknown;
            if (!changes.RootElement.TryGetProperty("provider", out var providerElement))
            {
                result.Code = "SaveBusinessNumber:17";
                result.Message = "Provider not found in changes.";
                return result;
            }
            if (!providerElement.TryGetInt32(out var providerInt))
            {
                result.Code = "SaveBusinessNumber:18";
                result.Message = "Invalid provider type.";
                return result;
            }
            if (!Enum.IsDefined(typeof(BusinessNumberProviderEnum), providerInt))
            {
                result.Code = "SaveBusinessNumber:19";
                result.Message = "Invalid provider type.";
                return result;
            }
            provider = (BusinessNumberProviderEnum)providerInt;

            BusinessNumberData? exisitingNumberData = null;
            if (postType == "new")
            {
                if (businessResult.Data.Permission.Numbers.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessAgent:20";
                    result.Message = "Business does not have permission to add new numbers";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Numbers.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Numbers.DisabledAddingReason;
                    }

                    return result;
                }

                bool numberExists = await _businessManager.GetNumberManager().CheckBusinessNumberExistsByNumber(countryCode, number, businessId);
                if (numberExists)
                {
                    result.Code = "SaveBusinessNumber:21";
                    result.Message = "Number already exists for business with same country code and number";
                    return result;
                }
            }
            else
            {
                if (businessResult.Data.Permission.Numbers.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessAgent:22";
                    result.Message = "Business does not have permission to edit numbers";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Numbers.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Numbers.DisabledEditingReason;
                    }

                    return result;
                }

                if (!formData.TryGetValue("numberId", out StringValues numberIdValue))
                {
                    result.Code = "SaveBusinessNumber:23";
                    result.Message = "Missing number id";
                    return result;
                }

                string? exisitingNumberId = numberIdValue.ToString();
                if (string.IsNullOrWhiteSpace(exisitingNumberId))
                {
                    result.Code = "SaveBusinessNumber:24";
                    result.Message = "Invalid number id";
                    return result;
                }

                exisitingNumberData = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, exisitingNumberId);
                if (exisitingNumberData == null)
                {
                    result.Code = "SaveBusinessNumber:25";
                    result.Message = "Number not found";
                    return result;
                }

                if (exisitingNumberData.CountryCode != countryCode || exisitingNumberData.Number != number || exisitingNumberData.Provider != provider)
                {
                    result.Code = "SaveBusinessNumber:26";
                    result.Message = "You are not allowed to edit a number's country code or number or provider";
                    return result;
                }
            }

            var saveResult = await _businessManager.GetNumberManager().AddOrUpdateBusinessNumber(
                changes, 
                countryCode,
                number,
                provider,
                postType,
                exisitingNumberData,
                businessId,
                _regionManager
            );

            if (!saveResult.Success)
            {
                result.Code = "SaveBusinessNumber:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }
    }
}

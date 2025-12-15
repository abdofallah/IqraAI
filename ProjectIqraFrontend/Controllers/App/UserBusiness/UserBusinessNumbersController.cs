using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PhoneNumbers;
using System.Text.Json;
using Twilio.TwiML.Voice;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessNumbersController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        
        public UserBusinessNumbersController(UserManager userManager, BusinessManager businessManager, RegionManager regionManager)
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

            UserData? user = await _userManager.GetFullUserByEmail(userEmail);
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

            // Get Provider First
            if (
                !changes.RootElement.TryGetProperty("provider", out var providerElement) ||
                !providerElement.TryGetInt32(out var providerInt) ||
                !Enum.IsDefined(typeof(TelephonyProviderEnum), providerInt)
            ) {
                return result.SetFailureResult(
                    "SaveBusinessNumber:MISSING_PROVIDER",
                    "Provider not found or invalid."
                );
            }
            var provider = (TelephonyProviderEnum)providerInt;

            // Get Integration Id
            if (!changes.RootElement.TryGetProperty("integrationId", out var integrationIdElement))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:MISSING_INTEGRATION",
                    "Integration ID not found in changes."
                );
            }
            string? integrationId = integrationIdElement.GetString();
            if (string.IsNullOrWhiteSpace(integrationId))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:EMPTY_INTEGRATION",
                    "Integration ID cannot be empty."
                );
            }

            // Get number
            if (!changes.RootElement.TryGetProperty("number", out var numberElement))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:MISSING_NUMBER",
                    "Number not found in changes."
                );
            }
            string? number = numberElement.GetString();
            if (string.IsNullOrWhiteSpace(number))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:EMPTY_NUMBER",
                    "Number cannot be empty."
                );
            }

            // Country Code
            string countryCode = "";
            bool isE164 = false;
            if (provider == TelephonyProviderEnum.SIP)
            {
                // Check IsE164 flag
                if (
                    !changes.RootElement.TryGetProperty("isE164Number", out var isE164El) ||
                    (isE164El.ValueKind != JsonValueKind.True && isE164El.ValueKind != JsonValueKind.False)
                ) {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:MISSING_IS_E164",
                        "IsE164 flag not found."
                    );
                }
                isE164 = isE164El.GetBoolean();

                if (isE164)
                {
                    if (
                        !changes.RootElement.TryGetProperty("countryCode", out var countryCodeElement) ||
                        countryCodeElement.ValueKind != JsonValueKind.String
                    ) {
                        return result.SetFailureResult(
                            "SaveBusinessNumber:MISSING_COUNTRY",
                            "Country code not found for E164 sip number in changes."
                        );
                    }

                    countryCode = countryCodeElement.GetString()!;
                }
            }
            else
            {
                // Get country code
                if (
                    !changes.RootElement.TryGetProperty("countryCode", out var countryCodeElement) ||
                    countryCodeElement.ValueKind != JsonValueKind.String
                ) {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:MISSING_COUNTRY",
                        "Country code not found in changes."
                    );
                }

                countryCode = countryCodeElement.GetString()!;
                isE164 = true;
            }

            // Validate Number
            if (isE164)
            {
                try
                {
                    PhoneNumber parsedPhoneNumber = PhoneNumberUtil.GetInstance().Parse(number, countryCode);
                    if (!PhoneNumberUtil.GetInstance().IsValidNumber(parsedPhoneNumber))
                    {
                        return result.SetFailureResult(
                            "SaveBusinessNumber:INVALID_NUMBER_FORMAT",
                            "Number validation failed for specified country."
                        );
                    }
                }
                catch
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:INVALID_NUMBER_PARSE_EXCEPTION",
                        "Could not parse phone number."
                    );
                }
            }

            BusinessNumberData? exisitingNumberData = null;
            if (postType == "new")
            {
                if (businessResult.Data.Permission.Numbers.DisabledAddingAt != null)
                {
                    var message = "Business does not have permission to add new numbers";
                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Numbers.DisabledAddingReason))
                    {
                        message += ": " + businessResult.Data.Permission.Numbers.DisabledAddingReason;
                    }

                    return result.SetFailureResult(
                        "SaveBusinessNumber:DISABLED_ADDING",
                        message
                    );
                }

                bool numberExists = await _businessManager.GetNumberManager().CheckBusinessNumberExistsByNumber(countryCode, number, businessId);
                if (numberExists)
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:NUMBER_EXISTS",
                        "Number already exists for business with same country code and number"
                    );
                }
            }
            else
            {
                if (businessResult.Data.Permission.Numbers.DisabledEditingAt != null)
                {
                    var message = "Business does not have permission to edit numbers";
                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Numbers.DisabledEditingReason))
                    {
                        message += ": " + businessResult.Data.Permission.Numbers.DisabledEditingReason;
                    }

                    return result.SetFailureResult(
                        "SaveBusinessNumber:DISABLED_EDITING",
                        message
                    );
                }

                if (!formData.TryGetValue("numberId", out StringValues numberIdValue))
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:MISSING_NUMBER_ID",
                        "Missing number id"
                    );
                }

                string? exisitingNumberId = numberIdValue.ToString();
                if (string.IsNullOrWhiteSpace(exisitingNumberId))
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:INVALID_NUMBER_ID",
                        "Invalid number id"
                    );
                }

                exisitingNumberData = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, exisitingNumberId);
                if (exisitingNumberData == null)
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:NUMBER_NOT_FOUND",
                        "Number not found"
                    );
                }

                if (exisitingNumberData.CountryCode != countryCode || exisitingNumberData.Number != number || exisitingNumberData.Provider != provider)
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:NOT_ALLOWED_TO_EDIT",
                        "You are not allowed to edit a number's country code or number or provider or integration"
                    );
                }
            }

            var saveResult = await _businessManager.GetNumberManager().AddOrUpdateBusinessNumber(
                changes, 
                countryCode,
                number,
                integrationId,
                provider,
                postType,
                exisitingNumberData,
                businessId,
                _regionManager,
                user.Permission.IsAdmin
            );

            if (!saveResult.Success)
            {
                return result.SetFailureResult(
                    $"SaveBusinessNumber:{saveResult.Code}",
                    result.Message
                );
            }

            return result.SetSuccessResult(saveResult.Data);
        }
    }
}

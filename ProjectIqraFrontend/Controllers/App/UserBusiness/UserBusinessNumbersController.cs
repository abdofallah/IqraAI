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
using ProjectIqraFrontend.Middlewares;
using System.Text.Json;
using Twilio.TwiML.Voice;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessNumbersController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        
        public UserBusinessNumbersController(
            UserManager userManager,
            BusinessManager businessManager,
            RegionManager regionManager,
            UserSessionValidationHelper userSessionValidationHelper
        ) {
            _userManager = userManager;
            _businessManager = businessManager;
            _regionManager = regionManager;
            _userSessionValidationHelper = userSessionValidationHelper;
        }

        [HttpPost("/app/user/business/{businessId}/numbers/save")]
        public async Task<FunctionReturnResult<BusinessNumberData?>> SaveBusinessNumber([FromForm] IFormCollection formData, long businessId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkUserBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"SaveBusinessTelephonyCampaign:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Numbers.DisabledFullAt != null)
            {
                var message = "Business does not have permission to access numbers";
                if (!string.IsNullOrEmpty(businessData.Permission.Numbers.DisabledFullReason))
                {
                    message += ": " + businessData.Permission.Numbers.DisabledFullReason;
                }

                return result.SetFailureResult(
                    "SaveBusinessNumber:BUSINESS_NUMBERS_DISABLED",
                    message
                );
            }

            if (!formData.TryGetValue("postType", out StringValues postTypeValue))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:MISSING_POST_TYPE",
                    "Post type not found in form data."
                );
            }

            string? postType = postTypeValue.ToString();
            if (string.IsNullOrWhiteSpace(postType)
                || postType != "new" && postType != "edit")
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:INVALID_POST_TYPE",
                    "Invalid post type."
                );
            }

            // Number Changes Data
            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:MISSING_CHANGES",
                    "Changes not found in form data."
                );
            }
            JsonDocument? changes;
            try
            {
                changes = JsonDocument.Parse(changesJsonString);
            }
            catch
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:INVALID_CHANGES",
                    "Invalid changes."
                );
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
                if (businessData.Permission.Numbers.DisabledAddingAt != null)
                {
                    var message = "Business does not have permission to add new numbers";
                    if (!string.IsNullOrEmpty(businessData.Permission.Numbers.DisabledAddingReason))
                    {
                        message += ": " + businessData.Permission.Numbers.DisabledAddingReason;
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
                if (businessData.Permission.Numbers.DisabledEditingAt != null)
                {
                    var message = "Business does not have permission to edit numbers";
                    if (!string.IsNullOrEmpty(businessData.Permission.Numbers.DisabledEditingReason))
                    {
                        message += ": " + businessData.Permission.Numbers.DisabledEditingReason;
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
                userData.Permission.IsAdmin
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

        [HttpPost("/app/user/business/{businessId}/numbers/{numberId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessNumber(long businessId, string numberId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkUserBusinessesEditingEnabled: true
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"DeleteBusinessNumber:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Numbers.DisabledFullAt != null)
            {
                var message = "Business does not have permission to access numbers";
                if (!string.IsNullOrEmpty(businessData.Permission.Numbers.DisabledFullReason))
                {
                    message += ": " + businessData.Permission.Numbers.DisabledFullReason;
                }

                return result.SetFailureResult(
                    "SaveBusinessNumber:BUSINESS_NUMBERS_DISABLED",
                    message
                );
            }

            var numberData = await _businessManager.GetNumberManager().GetBusinessNumberById(businessId, numberId);
            if (numberData == null)
            {
                return result.SetFailureResult(
                    "DeleteBusinessNumber:NUMBER_NOT_FOUND",
                    "Number not found"
                );
            }

            var deleteResult = await _businessManager.GetNumberManager().DeleteBusinessNumber(businessId, numberData);
            if (!deleteResult.Success)
            {
                return result.SetFailureResult(
                    $"DeleteBusinessNumber:{deleteResult.Code}",
                    deleteResult.Message
                );
            }

            return result.SetSuccessResult();
        }
    }
}

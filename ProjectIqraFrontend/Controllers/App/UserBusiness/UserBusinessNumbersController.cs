using IqraCore.Entities.Business;
using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using PhoneNumbers;
using System.Text.Json;
using static IqraCore.Interfaces.Validation.IUserBusinessPermissionHelper;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessNumbersController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessNumbersController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/numbers/save")]
        public async Task<FunctionReturnResult<BusinessNumberData?>> SaveBusinessNumber([FromForm] IFormCollection formData, long businessId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            try
            {
                // Check New or Edit
                string? postType = formData["postType"].ToString();
                if (
                    string.IsNullOrWhiteSpace(postType) ||
                    (postType != "new" && postType != "edit")
                )
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:INVALID_POST_TYPE",
                        "Invalid post type specified. Can only be 'new' or 'edit'."
                    );
                }

                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Numbers",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Numbers",
                        Type = postType == "new" ? BusinessModulePermissionType.Adding : BusinessModulePermissionType.Editing,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:" + userSessionAndBusinessValidationResult.Code,
                        userSessionAndBusinessValidationResult.Message
                    );
                }
                var userData = userSessionAndBusinessValidationResult.Data!.userData!;

                // Number Changes Data
                if (!formData.TryGetValue("changes", out var changesJsonStringValues))
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:MISSING_CHANGES",
                        "Changes not found in form data."
                    );
                }
                string? changesJsonString = changesJsonStringValues.ToString();
                if (string.IsNullOrWhiteSpace(changesJsonString))
                {
                    return result.SetFailureResult(
                        "SaveBusinessNumber:EMPTY_CHANGES",
                        "Changes cannot be empty."
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
                )
                {
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
                    )
                    {
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
                        )
                        {
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
                    )
                    {
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
                }

                var saveResult = await _businessManager.GetNumberManager().AddOrUpdateBusinessNumber(
                    changes,
                    isE164,
                    countryCode,
                    number,
                    integrationId,
                    provider,
                    postType,
                    exisitingNumberData,
                    businessId,
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "SaveBusinessNumber:EXCEPTION",
                    $"Failed to save business number: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/numbers/{numberId}/delete")]
        public async Task<FunctionReturnResult> DeleteBusinessNumber(long businessId, string numberId)
        {
            var result = new FunctionReturnResult<BusinessNumberData?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Numbers",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Numbers",
                        Type = BusinessModulePermissionType.Deleting,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"DeleteBusinessNumber:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
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
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "DeleteBusinessNumber:EXCEPTION",
                    $"Failed to delete business number: {ex.Message}"
                );
            }
        }
    }
}

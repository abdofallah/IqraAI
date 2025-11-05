using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.User.WhiteLabel.Customer;
using IqraCore.Entities.WhiteLabel;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using IqraInfrastructure.Repositories.User;
using IqraInfrastructure.Repositories.WhiteLabel;

namespace ProjectIqraFrontend.Middlewares
{
    public class ValidateUserAndBusinessResult
    {
        public UserData? userData { get; set; }
        public BusinessData? businessData { get; set; }
        public UserWhiteLabelCustomerData? userWhiteLabelCustomerData { get; set; }
    }

    public class ValidateWhiteLabelCustomerSessionResult
    {
        public string MasterUserEmail { get; set; }
        public string CustomerEmail { get; set; }
    }

    public class ValidateUserSessionAndGetUserAsync
    {
        public UserData? userData { get; set; }
        public UserWhiteLabelCustomerData? userWhiteLabelCustomerData { get; set; }
    }

    public class UserSessionValidationHelper
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly WhiteLabelCustomerSessionRepository _whiteLabelCustomerSessionRepository;
        private readonly UserRepository _userRepository;

        public UserSessionValidationHelper(
            UserManager userManager,
            BusinessManager businessManager,
            WhiteLabelCustomerSessionRepository wlSessionRepo,
            UserRepository userRepository
        )
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _whiteLabelCustomerSessionRepository = wlSessionRepo;
            _userRepository = userRepository;
        }

        public async Task<FunctionReturnResult<string?>> ValidateUserSessionAsync(HttpRequest Request)
        {
            var result = new FunctionReturnResult<string?>();

            // Validate session
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult("ValidateUserSessionAsync:INVALID_SESSION_DATA", "Invalid session data");
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                return result.SetFailureResult("ValidateUserSessionAsync:SESSION_VALIDATION_FAILED", "Session validation failed");
            }

            return result.SetSuccessResult(userEmail);
        }

        public async Task<FunctionReturnResult<ValidateWhiteLabelCustomerSessionResult?>> ValidateWhiteLabelCustomerSessionAsync(HttpRequest Request, WhiteLabelContext whiteLabelContext)
        {
            var result = new FunctionReturnResult<ValidateWhiteLabelCustomerSessionResult?>();

            try
            {
                if (!whiteLabelContext.IsWhiteLabelRequest)
                {
                    return result.SetFailureResult(
                        "ValidateWhiteLabelCustomerSessionAsync:NOT_WHITE_LABEL_REQUEST",
                        "Not a white-label request"
                    );
                }

                var sessionCookie = Request.Cookies["wl_session"];
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    return result.SetFailureResult(
                        "ValidateWhiteLabelCustomerSessionAsync:INVALID_SESSION_DATA",
                        "Invalid session data"
                    );
                }

                var sessionData = await _whiteLabelCustomerSessionRepository.RetrieveSessionAsync(sessionCookie);
                if (sessionData == null)
                {
                    return result.SetFailureResult(
                        "ValidateWhiteLabelCustomerSessionAsync:INVALID_SESSION_DATA",
                        "Session does not exist"
                    );
                }

                return result.SetSuccessResult(new ValidateWhiteLabelCustomerSessionResult()
                {
                    MasterUserEmail = sessionData.MasterUserEmail,
                    CustomerEmail = sessionData.CustomerEmail
                });
            }
            catch (Exception ex) {
                return result.SetFailureResult(
                    "ValidateWhiteLabelCustomerSessionAsync:EXCEPTION",
                    ex.Message
                );
            }
        }

        public async Task<FunctionReturnResult<ValidateUserSessionAndGetUserAsync>> ValidateUserSessionAndGetUserAsync(
            HttpRequest Request,
            bool checkUserDisabled = true,
            WhiteLabelContext? whiteLabelContext = null
        )
        {
            var result = new FunctionReturnResult<ValidateUserSessionAndGetUserAsync>();

            string? userEmail = null;
            string? whiteLabelCustomerEmail = null;

            if (whiteLabelContext != null && whiteLabelContext.IsWhiteLabelRequest)
            {
                var validateWhiteLabelCustomerSessionResult = await ValidateWhiteLabelCustomerSessionAsync(Request, whiteLabelContext);
                if (!validateWhiteLabelCustomerSessionResult.Success)
                {
                    return result.SetFailureResult(
                        $"ValidateUserSessionAndGetUserAsync:{validateWhiteLabelCustomerSessionResult.Code}",
                        validateWhiteLabelCustomerSessionResult.Message
                    );
                }

                userEmail = validateWhiteLabelCustomerSessionResult.Data!.MasterUserEmail;
                whiteLabelCustomerEmail = validateWhiteLabelCustomerSessionResult.Data!.CustomerEmail;
            }
            else
            {
                // Validate session
                var validateUserSessionResult = await ValidateUserSessionAsync(Request);
                if (!validateUserSessionResult.Success)
                {
                    return result.SetFailureResult(
                        $"ValidateUserSessionAndGetUserAsync:{validateUserSessionResult.Code}",
                        validateUserSessionResult.Message
                    );
                }
                userEmail = validateUserSessionResult.Data!;
            }

            // Get and validate user
            UserData? userData = await _userManager.GetFullUserByEmail(userEmail);
            if (userData == null)
            {
                return result.SetFailureResult("ValidateUserSessionAndGetUserAsync:USER_DATA_NOT_FOUND", "User not found");
            }

            if (checkUserDisabled && userData.Permission.DisableUserAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAsync:USER_DISABLED",
                    $"User is disabled{(string.IsNullOrEmpty(userData.Permission.UserDisabledReason) ? "" : ": " + userData.Permission.UserDisabledReason)}"
                );
            }

            // Get and validate white label customer
            UserWhiteLabelCustomerData? userWhiteLabelCustomerData = null;
            if (whiteLabelContext != null && whiteLabelContext.IsWhiteLabelRequest)
            {
                userWhiteLabelCustomerData = userData.WhiteLabel.Customers.FirstOrDefault(c => c.Email == whiteLabelCustomerEmail);
                if (userWhiteLabelCustomerData == null)
                {
                    return result.SetFailureResult(
                        "ValidateUserSessionAndGetUserAsync:WHITE_LABEL_CUSTOMER_NOT_FOUND",
                        "White label customer not found"
                    );
                }

                if (checkUserDisabled && userWhiteLabelCustomerData.Permission.DisabledAt != null)
                {
                    return result.SetFailureResult(
                        "ValidateUserSessionAndGetUserAsync:WHITE_LABEL_CUSTOMER_DISABLED",
                        $"White label customer is disabled{(string.IsNullOrEmpty(userWhiteLabelCustomerData.Permission.DisabledReason) ? "" : ": " + userWhiteLabelCustomerData.Permission.DisabledReason)}"
                    );
                }
            }

            return result.SetSuccessResult(new ValidateUserSessionAndGetUserAsync()
            {
                userData = userData,
                userWhiteLabelCustomerData = userWhiteLabelCustomerData
            });
        }

        public async Task<FunctionReturnResult<ValidateUserAndBusinessResult?>> ValidateUserSessionAndGetUserAndBusinessAsync(
            HttpRequest Request,
            long businessId,

            bool checkUserDisabled = true,       
            
            bool checkUserBusinessesDisabled = true,
            bool checkUserBusinessesAddingEnabled = false,
            bool checkUserBusinessesEditingEnabled = false,
            bool checkUserBusinessesDeletingEnabled = false,

            bool checkBusinessIsDisabled = true,
            bool checkBusinessCanBeEdited = false,
            bool checkBusinessCanBeDeleted = false,

            WhiteLabelContext? whiteLabelContext = null
        )
        {
            var result = new FunctionReturnResult<ValidateUserAndBusinessResult?>();

            var userSessionValidationResult = await ValidateUserSessionAndGetUserAsync(Request, checkUserDisabled, whiteLabelContext);
            if (!userSessionValidationResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndGetUserAndBusinessAsync:{userSessionValidationResult.Code}",
                    userSessionValidationResult.Message
                );
            }
            var userData = userSessionValidationResult.Data!.userData!;
            var whiteLabelCustomerData = userSessionValidationResult.Data!.userWhiteLabelCustomerData;

            // Check User Businesses Full Enabled
            if (checkUserBusinessesDisabled && userData.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_DISABLED",
                    $"Bussinesses are disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.DisableBusinessesReason) ? "" : ": " + userData.Permission.Business.DisableBusinessesReason)}"
                );
            }

            // Check User Businesses Adding Enabled
            if (checkUserBusinessesAddingEnabled && userData.Permission.Business.AddBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_ADDING_DISABLED",
                    $"Bussinesses adding is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.AddBusinessDisableReason) ? "" : ": " + userData.Permission.Business.AddBusinessDisableReason)}"
                );
            }

            // Check User Businesses Editing Enabled
            if (checkUserBusinessesEditingEnabled && userData.Permission.Business.EditBusinessDisabledAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_EDITING_DISABLED",
                    $"Bussinesses editing is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.EditBusinessDisableReason) ? "" : ": " + userData.Permission.Business.EditBusinessDisableReason)}"
                );
            }

            // Check User Businesses Deleting Enabled
            if (checkUserBusinessesDeletingEnabled && userData.Permission.Business.DeleteBusinessDisableAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:USER_BUSINESSES_DELETING_DISABLED",
                    $"Bussinesses deleting is disabled for the user{(string.IsNullOrWhiteSpace(userData.Permission.Business.DeleteBusinessDisableReason) ? "" : ": " + userData.Permission.Business.DeleteBusinessDisableReason)}"
                );
            }

            // Get and validate business
            var businessGetResult = await _businessManager.GetUserBusinessById(businessId, userData.Email);
            if (!businessGetResult.Success)
            {
                return result.SetFailureResult(
                    $"ValidateUserSessionAndGetUserAndBusinessAsync:{businessGetResult.Code}",
                    businessGetResult.Message
                );
            }
            var businessData = businessGetResult.Data!;

            // Check Business Full Disabled
            if (checkBusinessIsDisabled && businessData.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DISABLED",
                    $"Business is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledFullReason) ? "" : ": " + businessData.Permission.DisabledFullReason)}"
                );
            }

            // Check Business Editing Disabled
            if (checkBusinessCanBeEdited && businessData.Permission.DisabledEditingAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_EDITING_DISABLED",
                    $"Business editing is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledEditingReason) ? "" : ": " + businessData.Permission.DisabledEditingReason)}"
                );
            }

            // Check Business Deleting Disabled
            if (checkBusinessCanBeDeleted && businessData.Permission.DisabledDeletingAt != null)
            {
                return result.SetFailureResult(
                    "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_DELETING_DISABLED",
                    $"Business deleting is disabled{(string.IsNullOrWhiteSpace(businessData.Permission.DisabledDeletingReason) ? "" : ": " + businessData.Permission.DisabledDeletingReason)}"
                );
            }

            // VALIDATE WHITE LABEL CUSTOMER BUSINESS RELATED ETC
            if (whiteLabelContext != null && whiteLabelContext.IsWhiteLabelRequest)
            {
                if (!whiteLabelCustomerData!.AssignedBusinesses.Contains(businessId))
                {
                    return result.SetFailureResult(
                        "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_NOT_ASSIGNED_TO_WHITE_LABEL_CUSTOMER",
                        $"Business is not assigned to the white label customer"
                    );
                }

                if (businessData.WhiteLabelAssignedCustomerEmail != whiteLabelCustomerData.Email)
                {
                    return result.SetFailureResult(
                        "ValidateUserSessionAndGetUserAndBusinessAsync:BUSINESS_NOT_ASSIGNED_TO_WHITE_LABEL_CUSTOMER",
                        $"Business is not assigned to the white label customer"
                    );
                }

                // TODO in future check for other permissions
            }

            return result.SetSuccessResult(new ValidateUserAndBusinessResult() { userData = userData, userWhiteLabelCustomerData = whiteLabelCustomerData, businessData = businessData });
        }
    }
}

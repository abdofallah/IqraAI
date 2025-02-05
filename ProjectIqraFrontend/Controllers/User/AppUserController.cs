using IqraCore.Entities.Business;
using IqraCore.Entities.Business.WhiteLabelDomain;
using IqraCore.Entities.Helper.Business;
using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Number;
using IqraCore.Entities.User;
using IqraCore.Models.AppUser;
using IqraCore.Utilities;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.User
{
    public class AppUserController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly NumberManager _numberManager;

        public AppUserController(UserManager userManager, BusinessManager businessManager, NumberManager numberManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _numberManager = numberManager;
        }

        /**
         * 
         * User
         * 
        **/

        [HttpPost("/app/user/permissions/business")]
        public async Task<FunctionReturnResult<UserPermissionBusiness?>> GetUserBussinessPermissions()
        {
            var result = new FunctionReturnResult<UserPermissionBusiness?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBussinessPermissions:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBussinessPermissions:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBussinessPermissions:3";
                result.Message = "User not found";
                return result;
            }

            result.Success = true;
            result.Data = user.Permission.Business;

            return result;
        }

        /**
         * 
         * Business
         * 
        **/

        [HttpPost("/app/user/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinesses()
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinesses:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            FunctionReturnResult<List<BusinessData>?> businessesResult = await _businessManager.GetUserBusinessesByIds(user.Businesses, user.Email);
            if (!businessesResult.Success)
            {
                result.Code = "GetUserBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/meta")]
        public async Task<FunctionReturnResult<BusinessData?>> GetUserBusinessMeta(long businessId)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessMeta:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessMeta:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessMeta:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessMeta:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessMeta:5";
                result.Message = "User does not own this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessMeta:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessMeta:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            result.Success = true;
            result.Data = businessResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/app")]
        public async Task<FunctionReturnResult<BusinessApp?>> GetUserBusinessApp(long businessId)
        {
            var result = new FunctionReturnResult<BusinessApp?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessApp:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessApp:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessApp:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessApp:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessApp:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessApp:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessApp:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, user.Email);
            if (!businessAppResult.Success)
            {
                result.Code = "GetUserBusinessApp:" + businessAppResult.Code;
                result.Message = businessAppResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessAppResult.Data;

            return result;
        }

        [HttpPost("/app/user/business/{businessId}/whitelabeldomains")]
        public async Task<FunctionReturnResult<List<BusinessWhiteLabelDomain>?>> GetUserBusinessWhiteLabelDomains(long businessId)
        {
            var result = new FunctionReturnResult<List<BusinessWhiteLabelDomain>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetSettingsManager().GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = "GetUserBusinessWhiteLabelDomains:" + businessWhiteLabelDomainResult.Code;
                result.Message = businessWhiteLabelDomainResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessWhiteLabelDomainResult.Data;
            return result;
        }

        [HttpPost("/app/user/business/{businessId}")]
        public async Task<FunctionReturnResult<GetUserBusinessFullReturnModel?>> GetUserBusiness(long businessId)
        {
            var result = new FunctionReturnResult<GetUserBusinessFullReturnModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserBusiness:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserBusiness:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserBusiness:3";
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetUserBusiness:4";
                result.Message = "User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason);
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetUserBusiness:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetUserBusiness:6";
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<BusinessApp?> businessAppResult = await _businessManager.GetUserBusinessAppById(businessId, user.Email);
            if (!businessAppResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessAppResult.Code;
                result.Message = businessAppResult.Message;
                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetSettingsManager().GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = "GetUserBusiness:" + businessWhiteLabelDomainResult.Code;
                result.Message = businessWhiteLabelDomainResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = new GetUserBusinessFullReturnModel()
            {
                BusinessData = businessResult.Data,
                BusinessApp = businessAppResult.Data,
                BusinessWhiteLabelDomain = businessWhiteLabelDomainResult.Data
            };

            return result;
        }

        [HttpPost("/app/user/business/add")]
        public async Task<FunctionReturnResult<BusinessData?>> AddUserBusiness([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "AddUserBusiness:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "AddUserBusiness:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "AddUserBusiness:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.AddBusinessDisabledAt != null)
            {
                result.Code = "AddUserBusiness:4";
                result.Message = "User does not have permission to add businesses";

                if (user.Permission.Business.DisableBusinessesAt != null && !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.AddBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.AddBusinessDisableReason;
                }

                return result;
            }

            string? businessName = formData["BusinessName"];
            string? businessType = formData["BusinessType"];
            IFormFile? businessLogo = formData.Files.GetFile("BusinessLogo");

            if (string.IsNullOrWhiteSpace(businessName) || businessName.Length > 64)
            {
                result.Code = "AddUserBusiness:5";
                result.Message = "Invalid business name. Minimum length is 1 and maximum length is 64.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(businessType))
            {
                result.Code = "AddUserBusiness:6";
                result.Message = "Missing business type";
                return result;
            }

            if (!int.TryParse(businessType, out int businessTypeInt) || !Enum.IsDefined(typeof(BusinessTypeEnum), businessTypeInt))
            {
                result.Code = "AddUserBusiness:7";
                result.Message = "Invalid business type";
                return result;
            }

            BusinessTypeEnum businessTypeEnum = (BusinessTypeEnum)businessTypeInt;
            if (businessTypeEnum != BusinessTypeEnum.NoCode)
            {
                result.Code = "AddUserBusiness:8";
                result.Message = "Business type not supported";
                return result;
            }

            if (businessLogo != null)
            {
                int imageResult = ImageHelper.ValidateBusinessLogoFile(businessLogo);
                if (imageResult == 0)
                {
                    result.Code = "AddUserBusiness:9";
                    result.Message = "Business logo too large. Allowed file size is 5MB.";
                    return result;
                }

                if (imageResult == 1)
                {
                    result.Code = "AddUserBusiness:10";
                    result.Message = "Invalid business logo file. Allowed file types are: png, jpg, jpeg, webp, gif.";
                    return result;
                }

                if (imageResult != 200)
                {
                    result.Code = "AddUserBusiness:11";
                    result.Message = "Failed to validate business logo.";
                    return result;
                }
            }

            var newBusinessResult = await _businessManager.AddBusiness(
                new BusinessData()
                {
                    Name = businessName,
                    MasterUserEmail = userEmail,
                    Type = businessTypeEnum,
                    Tutorials = new Dictionary<string, object>()
                    {
                        { "NewBusinessTutorial", true}
                    }
                },
                businessLogo
            );
            if (!newBusinessResult.Success)
            {
                result.Code = "AddUserBusiness:" + newBusinessResult.Code;
                result.Message = newBusinessResult.Message;
                return result;
            }

            await _userManager.AddBusinessIdToUser(userEmail, newBusinessResult.Data.Id);

            result.Success = true;
            result.Data = newBusinessResult.Data;
            return result;
        }


        /**
         * 
         * Numbers
         * 
        **/

        [HttpPost("/app/user/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(int provider)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserNumbers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserNumbers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserNumbers:3";
                result.Message = "User not found";
                return result;
            }

            if (!Enum.IsDefined(typeof(NumberProviderEnum), provider))
            {
                result.Code = "GetUserNumbers:4";
                result.Message = "Invalid provider";
                return result;
            }

            FunctionReturnResult<List<NumberData>?> numbersResult = await _numberManager.GetUserNumbers(user.Email);
            if (!numbersResult.Success)
            {
                result.Code = "GetUserNumbers:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

        [HttpPost("/app/user/numbers/{provider}")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbersByProvider(int provider, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUserNumbersByProvider:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetUserNumbersByProvider:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUserNumbersByProvider:3";
                result.Message = "User not found";
                return result;
            }

            if (!Enum.IsDefined(typeof(NumberProviderEnum), provider))
            {
                result.Code = "GetUserNumbersByProvider:4";
                result.Message = "Invalid provider";
                return result;
            }

            FunctionReturnResult<List<NumberData>?> numbersResult = await _numberManager.GetUserNumbersByProvider((NumberProviderEnum)provider, user.Email, page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = "GetUserNumbersByProvider:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

    }
}

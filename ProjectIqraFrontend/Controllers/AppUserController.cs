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

namespace ProjectIqraFrontend.Controllers
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = 4;
                result.Message = ("User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason));
                return result;
            }

            FunctionReturnResult<List<BusinessData>?> businessesResult = await _businessManager.GetUserBusinessesByIds(user.Businesses, user.Email);
            if (!businessesResult.Success)
            {
                result.Code = 1000 + businessesResult.Code;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = 4;
                result.Message = ("User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason));
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = 5;
                result.Message = "User does not have permission to view this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = 1000 + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = 6;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = 4;
                result.Message = ("User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason));
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = 5;
                result.Message = "User does not have permission to view this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = 1000 + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = 6;
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
                result.Code = 2000 + businessAppResult.Code;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = 4;
                result.Message = ("User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason));
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = 5;
                result.Message = "User does not have permission to view this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = 1000 + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = 6;
                result.Message = "Business is disabled.";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += " Reason: " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = 1000 + businessWhiteLabelDomainResult.Code;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            UserPermission userPermission = user.Permission;
            if (userPermission.Business.DisableBusinessesAt != null)
            {
                result.Code = 4;
                result.Message = ("User does not have permission to view businesses" + (string.IsNullOrEmpty(userPermission.Business.DisableBusinessesReason) ? "" : ": " + userPermission.Business.DisableBusinessesReason));
                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = 5;
                result.Message = "User does not have permission to view this business";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, user.Email);
            if (!businessResult.Success)
            {
                result.Code = 1000 + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = 6;
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
                result.Code = 2000 + businessAppResult.Code;
                result.Message = businessAppResult.Message;
                return result;
            }

            FunctionReturnResult<List<BusinessWhiteLabelDomain>?> businessWhiteLabelDomainResult = await _businessManager.GetUserBusinessWhiteLabelDomainByIds(businessResult.Data.WhiteLabelDomainIds, businessId, user.Email);
            if (!businessWhiteLabelDomainResult.Success)
            {
                result.Code = 3000 + businessWhiteLabelDomainResult.Code;
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
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.AddBusinessDisabledAt != null)
            {
                result.Code = 4;
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
                result.Code = 6;
                result.Message = "Invalid business name. Minimum length is 1 and maximum length is 64.";
                return result;
            }

            if (string.IsNullOrWhiteSpace(businessType))
            {
                result.Code = 7;
                result.Message = "Missing business type";
                return result;
            }

            if (!int.TryParse(businessType, out int businessTypeInt) || !Enum.IsDefined(typeof(BusinessTypeEnum), businessTypeInt))
            {
                result.Code = 8;
                result.Message = "Invalid business type";
                return result;
            }

            BusinessTypeEnum businessTypeEnum = (BusinessTypeEnum)businessTypeInt;
            if (businessTypeEnum != BusinessTypeEnum.NoCode)
            {
                result.Code = 9;
                result.Message = "Business type not supported";
                return result;
            }

            if (businessLogo != null)
            {
                int imageResult = ImageHelper.ValidateBusinessLogoFile(businessLogo);
                if (imageResult == 0)
                {
                    result.Code = 11;
                    result.Message = "Business logo too large. Allowed file size is 5MB.";
                    return result;
                }
                
                if (imageResult == 1)
                {
                    result.Code = 12;
                    result.Message = "Invalid business logo file. Allowed file types are: png, jpg, jpeg, webp, gif.";
                    return result;
                }

                if (imageResult != 200)
                {
                    result.Code = 13;
                    result.Message = "Failed to validate business logo.";
                    return result;
                }
            }

            BusinessData newBusinessData = await _businessManager.AddBusiness(
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

            await _userManager.AddBusinessIdToUser(userEmail, newBusinessData.Id);
            
            result.Success = true;
            result.Data = newBusinessData;
            return result;
        }

        // Settings
        [HttpPost("/app/user/business/{businessId}/settings/save")]
        public async Task<FunctionReturnResult<bool?>> SaveBusinessSettings(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<bool?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = 4;
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

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = 1000 + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = 5;
                result.Message = "Business does not have permission to edit settings";

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
                result.Code = 2000 + updateResult.Code;
                result.Message = updateResult.Message;
                return result;
            }

            result.Success = true;
            return result;
        }



        /**
         * 
         * Numbers
         * 
        **/

        [HttpPost("/app/user/numbers/{provider}")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(int provider, int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = 1;
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = 2;
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = 3;
                result.Message = "User not found";
                return result;
            }

            if (!Enum.IsDefined(typeof(NumberProviderEnum), provider))
            {
                result.Code = 4;
                result.Message = "Invalid provider";
                return result;
            }

            FunctionReturnResult<List<NumberData>?> numbersResult = await _numberManager.GetUserNumbersByProvider((NumberProviderEnum)provider, user.Email, page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = 1000 + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }
    
    }
}

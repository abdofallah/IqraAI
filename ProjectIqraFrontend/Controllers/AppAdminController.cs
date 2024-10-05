using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Number;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Interfaces;
using IqraCore.Entities.Languages;
using IqraCore.Entities.LLM;
using IqraCore.Entities.Number;
using IqraCore.Entities.Region;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.App;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Languages;
using IqraInfrastructure.Services.LLM;
using IqraInfrastructure.Services.Number;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers
{
    public class AppAdminController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        private readonly NumberManager _numberManager;
        private readonly LLMProviderManager _llmProviderManager;
        private readonly LanguagesManager _languagesManager;

        public AppAdminController(UserManager userManager, BusinessManager businessManager, RegionManager regionManager, NumberManager numberManager, LLMProviderManager llmProviderManager, LanguagesManager languagesManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _regionManager = regionManager;
            _numberManager = numberManager;
            _llmProviderManager = llmProviderManager;
            _languagesManager = languagesManager;
        }

        /**
         * 
         * Users
         * 
        **/

        [HttpPost("/app/admin/users")]
        public async Task<FunctionReturnResult<List<UserData>?>> GetUsers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<UserData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUsers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetUsers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUsers:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUsers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var usersResult = await _userManager.GetUsersAsync(page, pageSize);
            if (!usersResult.Success)
            {
                result.Code = "GetUsers:" + usersResult.Code;
                result.Message = usersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = usersResult.Data;

            return result;
        }

        [HttpPost("/app/admin/user")]
        public async Task<FunctionReturnResult<UserData?>> GetUser(string email)
        {
            var result = new FunctionReturnResult<UserData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetUser:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetUser:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetUser:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUser:4";
                result.Message = "User is not an admin";
                return result;
            }

            var resultUser = await _userManager.GetUserByEmail(email);
            if (resultUser == null)
            {
                result.Code = "GetUser:5";
                result.Message = "User not found";
                return result;
            }

            result.Success = true;
            result.Data = resultUser;

            return result;
        }

        [HttpPost("/app/admin/user/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetUserBusinesses(string inputUserEmail, List<long> businessIds)
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

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
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

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUserBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetUserBusinessesByIds(businessIds, inputUserEmail);
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

        [HttpPost("/app/admin/user/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetUserNumbers(string inputUserEmail, List<string> numberIds)
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

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
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

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetUserNumbers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var numbersResult = await _numberManager.GetUserNumberByIds(numberIds, inputUserEmail);
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

        /**
         * 
         * Businesses
         * 
        **/

        [HttpPost("/app/admin/businesses")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> GetBusinesses(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            var businessesResult = await _businessManager.GetBusinesses(page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = "GetBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/admin/business/search")]
        public async Task<FunctionReturnResult<List<BusinessData>?>> SearchBusinesses(string query, int pageSize = 10, int page = 0)
        {
            var result = new FunctionReturnResult<List<BusinessData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SearchBusinesses:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SearchBusinesses:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SearchBusinesses:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SearchBusinesses:4";
                result.Message = "User is not an admin";
                return result;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                result.Code = "SearchBusinesses:5";
                result.Message = "Query cannot be empty";
                return result;
            }

            var businessesResult = await _businessManager.SearchBusinesses(query, page, pageSize);
            if (!businessesResult.Success)
            {
                result.Code = "SearchBusinesses:" + businessesResult.Code;
                result.Message = businessesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = businessesResult.Data;

            return result;
        }

        [HttpPost("/app/admin/business/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetBusinessNumbers(long businessId, List<string> numberIds)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinessNumbers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetBusinessNumbers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinessNumbers:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetBusinessNumbers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var numbersResult = await _numberManager.GetBusinessNumberByIds(numberIds, businessId);
            if (!numbersResult.Success)
            {
                result.Code = "GetBusinessNumbers:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

        /**
         * 
         * Regions
         * 
        **/

        [HttpPost("/app/admin/regions")]
        public async Task<FunctionReturnResult<List<RegionData>?>> GetRegions(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<RegionData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetRegions:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetRegions:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetRegions:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetRegions:4";
                result.Message = "User is not an admin";
                return result;
            }

            var regionsResult = await _regionManager.GetRegions(page, pageSize);
            if (!regionsResult.Success)
            {
                result.Code = "GetRegions:" + regionsResult.Code;
                result.Message = regionsResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = regionsResult.Data;

            return result;
        }

        /**
         * 
         * Numbers
         * 
        **/

        [HttpPost("/app/admin/numbers")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbers(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetNumbers:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetNumbers:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetNumbers:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetNumbers:4";
                result.Message = "User is not an admin";
                return result;
            }

            var numbersResult = await _numberManager.GetNumbers(page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = "GetNumbers:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

        [HttpPost("/app/admin/numbers/{provider}")]
        public async Task<FunctionReturnResult<List<NumberData>?>> GetNumbersByType(int provider, int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<NumberData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetNumbersByType:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetNumbersByType:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetNumbersByType:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetNumbersByType:4";
                result.Message = "User is not an admin";
                return result;
            }

            if (!Enum.IsDefined(typeof(NumberProviderEnum), provider))
            {
                result.Code = "GetNumbersByType:5";
                result.Message = "Invalid provider";
                return result;
            }

            var numbersResult = await _numberManager.GetNumbersByProvider((NumberProviderEnum)provider, page, pageSize);
            if (!numbersResult.Success)
            {
                result.Code = "GetNumbersByType:" + numbersResult.Code;
                result.Message = numbersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = numbersResult.Data;

            return result;
        }

        /**
         * 
         * LLM Providers
         * 
        **/

        [HttpPost("/app/admin/llmproviders")]
        public async Task<FunctionReturnResult<List<LLMProviderData>?>> GetLLMProviders(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LLMProviderData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetLLMProviders:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetLLMProviders:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetLLMProviders:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetLLMProviders:4";
                result.Message = "User is not an admin";
                return result;
            }

            var providersResult = await _llmProviderManager.GetProviderList(page, pageSize);
            if (!providersResult.Success)
            {
                result.Code = "GetLLMProviders:" + providersResult.Code;
                result.Message = providersResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = providersResult.Data;
            return result;
        }

        [HttpPost("/app/admin/llmproviders/save")]
        public async Task<FunctionReturnResult<LLMProviderData?>> SaveLLMProvider(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLLMProvider:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLLMProvider:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLLMProvider:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLLMProvider:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveLLMProvider:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveLLMProvider:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
            if (provider == null)
            {
                result.Code = "SaveLLMProvider:7";
                result.Message = "Provider not found";
                return result;
            }

            var saveResult = await _llmProviderManager.UpdateProvider(provider, formData);
            if (!saveResult.Success)
            {
                result.Code = "SaveLLMProvider:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

        [HttpPost("/app/admin/llmproviders/model/save")]
        public async Task<FunctionReturnResult<LLMProviderModelData?>> SaveLLMProviderModel(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LLMProviderModelData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLLMProviderModel:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLLMProviderModel:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLLMProviderModel:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLLMProviderModel:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? providerId = formData["providerId"];
            if (string.IsNullOrEmpty(providerId))
            {
                result.Code = "SaveLLMProviderModel:5";
                result.Message = "Provider id is required";
                return result;
            }

            if (!Enum.TryParse(typeof(InterfaceLLMProviderEnum), providerId, true, out object? providerIdEnum))
            {
                result.Code = "SaveLLMProviderModel:6";
                result.Message = "Invalid provider id enum";
                return result;
            }

            LLMProviderData? provider = await _llmProviderManager.GetProviderData(((InterfaceLLMProviderEnum)providerIdEnum));
            if (provider == null)
            {
                result.Code = "SaveLLMProviderModel:7";
                result.Message = "Provider not found";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "edit" && postType != "new"))
            {
                result.Code = "SaveLLMProviderModel:8";
                result.Message = "Post type is required or is not edit or new";
                return result;
            }

            string? modelId = formData["modelId"];
            if (string.IsNullOrEmpty(modelId))
            {
                result.Code = "SaveLLMProviderModel:9";
                result.Message = "Model id is required";
                return result;
            }

            LLMProviderModelData? oldModelData = provider.Models.Find(m => m.Id == modelId);
            if (postType == "edit")
            {
                if (oldModelData == null) {
                    result.Code = "SaveLLMProviderModel:10";
                    result.Message = "Model not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                if (oldModelData != null)
                {
                    result.Code = "SaveLLMProviderModel:11";
                    result.Message = "Model already exists";
                    return result;
                }
            }

            var saveResult = await _llmProviderManager.AddUpdateProviderModel(provider, modelId, postType, oldModelData, formData);
            if (!saveResult.Success)
            {
                result.Code = "SaveLLMProviderModel:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

        /**
         * 
         * Languages
         * 
        **/

        [HttpPost("/app/admin/languages")]
        public async Task<FunctionReturnResult<List<LanguagesData>?>> GetLanguages(int page = 0, int pageSize = 10)
        {
            var result = new FunctionReturnResult<List<LanguagesData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetLanguages:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetLanguages:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetLanguages:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetLanguages:4";
                result.Message = "User is not an admin";
                return result;
            }

            var languagesResult = await _languagesManager.GetLanguagesList(page, pageSize);
            if (!languagesResult.Success)
            {
                result.Code = "GetLanguages:" + languagesResult.Code;
                result.Message = languagesResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = languagesResult.Data;
            return result;
        }

        [HttpPost("/app/admin/languages/save")]
        public async Task<FunctionReturnResult<LanguagesData?>> SaveLanguage(IFormCollection formData)
        {
            var result = new FunctionReturnResult<LanguagesData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveLanguage:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveLanguage:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveLanguage:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveLanguage:4";
                result.Message = "User is not an admin";
                return result;
            }

            string? postType = formData["postType"];
            if (string.IsNullOrEmpty(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveLanguage:5";
                result.Message = "postType is required or is invalid";
                return result;
            }

            string? languageCode = formData["languageCode"];
            if (string.IsNullOrEmpty(languageCode))
            {
                result.Code = "SaveLanguage:6";
                result.Message = "languageCode is required";
                return result;
            }

            var languagesDataResult = await _languagesManager.GetLanguageByCode(languageCode);

            if (postType == "new")
            {
                if (languagesDataResult.Data != null)
                {
                    result.Code = "SaveLanguage:7";
                    result.Message = "Language already exists";
                    return result;
                }
            }
            else if (postType == "edit")
            {
                if (languagesDataResult.Data == null)
                {
                    result.Code = "SaveLanguage:8";
                    result.Message = "Language not found";
                    return result;
                }
            }

            var languageAddUpdateResult = await _languagesManager.AddUpdateLanguage(formData, postType, languageCode, languagesDataResult.Data);
            if (!languageAddUpdateResult.Success)
            {
                result.Code = "SaveLanguage:" + languageAddUpdateResult.Code;
                result.Message = languageAddUpdateResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = languageAddUpdateResult.Data;
            return result;
        }
    }
}

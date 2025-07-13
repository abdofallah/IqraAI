using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Entities.User;
using IqraCore.Utilities;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers.Admin
{
    public class AppAdminIntegrationsController : Controller
    {
        private readonly IntegrationsManager _integrationsManager;
        private readonly UserManager _userManager;

        public AppAdminIntegrationsController(IntegrationsManager integrationsManager, UserManager userManager)
        {
            _integrationsManager = integrationsManager;
            _userManager = userManager;
        }

        [HttpGet("/app/admin/integrations")]
        public async Task<FunctionReturnResult<List<IntegrationData>?>> GetIntegrations(int page = 0, int pageSize = 100)
        {
            var result = new FunctionReturnResult<List<IntegrationData>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetIntegrations:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "GetIntegrations:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetIntegrations:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "GetIntegrations:4";
                result.Message = "User is not an admin";
                return result;
            }

            var integrationsResult = await _integrationsManager.GetIntegrationsList(page, pageSize);
            if (!integrationsResult.Success)
            {
                result.Code = "GetIntegrations:" + integrationsResult.Code;
                result.Message = integrationsResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = integrationsResult.Data;
            return result;
        }

        [HttpPost("/app/admin/integrations/save")]
        public async Task<FunctionReturnResult<IntegrationData?>> SaveIntegration([FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<IntegrationData?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveIntegration:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveIntegration:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveIntegration:3";
                result.Message = "User not found";
                return result;
            }

            if (!user.Permission.IsAdmin)
            {
                result.Code = "SaveIntegration:4";
                result.Message = "User is not an admin";
                return result;
            }

            if (!formData.TryGetValue("changes", out var changesJsonString))
            {
                result.Code = "SaveIntegration:5";
                result.Message = "Changes data not found";
                return result;
            }

            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveIntegration:6";
                result.Message = "Invalid post type";
                return result;
            }

            formData.TryGetValue("currentIntegrationId", out StringValues currentIntegrationIdValue);
            string? currentIntegrationId = currentIntegrationIdValue.ToString();

            if (postType == "edit")
            {
                if (string.IsNullOrWhiteSpace(currentIntegrationId))
                {
                    result.Code = "SaveIntegration:7";
                    result.Message = "Missing existing integration id";
                    return result;
                }

                bool integrationExists = await _integrationsManager.IntegrationExists(currentIntegrationId);
                if (!integrationExists)
                {
                    result.Code = "SaveIntegration:8";
                    result.Message = "Integration not found";
                    return result;
                }
            }
            else if (postType == "new")
            {
                bool integrationExists = await _integrationsManager.IntegrationExists(currentIntegrationId);
                if (integrationExists)
                {
                    result.Code = "SaveIntegration:8";
                    result.Message = "Integration already exists with id.";
                    return result;
                }
            }

            // Handle logo file if present
            IFormFile? integrationLogo = formData.Files.FirstOrDefault(x => x.Name == "logo");
            if (integrationLogo == null && postType == "new")
            {
                result.Code = "SaveIntegration:8";
                result.Message = "Missing logo file";
                return result;
            }
            else if (integrationLogo != null)
            {
                int logoValidateResult = ImageHelper.ValidateBusinessLogoFile(integrationLogo);

                if (logoValidateResult == 0)
                {
                    result.Code = "SaveIntegration:9";
                    result.Message = "The integration logo file is too big. Maximum size is 5MB.";
                    return result;
                }
                else if (logoValidateResult == 1)
                {
                    result.Code = "SaveIntegration:10";
                    result.Message = "The integration logo file is not valid.";
                    return result;
                }
                else if (logoValidateResult != 200)
                {
                    result.Code = "SaveIntegration:11";
                    result.Message = "The integration logo file is not valid.";
                    return result;
                }
            }

            var saveResult = await _integrationsManager.AddOrUpdateIntegration(
                changesJsonString.ToString(),
                postType,
                currentIntegrationId,
                integrationLogo
            );

            if (!saveResult.Success)
            {
                result.Code = "SaveIntegration:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }

    }
}

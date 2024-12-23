using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Integrations;
using IqraCore.Entities.User;
using IqraInfrastructure.Services.Business;
using IqraInfrastructure.Services.Integrations;
using IqraInfrastructure.Services.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ProjectIqraFrontend.Controllers
{
    public class AppUserBusinessIntegrationsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly IntegrationsManager _integrationsManager;

        public AppUserBusinessIntegrationsController(
            UserManager userManager,
            BusinessManager businessManager,
            IntegrationsManager integrationsManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
            _integrationsManager = integrationsManager;
        }

        [HttpPost("/app/user/business/{businessId}/integrations/save")]
        public async Task<FunctionReturnResult<BusinessAppIntegration?>> SaveBusinessIntegration(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult<BusinessAppIntegration?>();

            // Session validation
            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "SaveBusinessIntegration:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!(await _userManager.ValidateSession(userEmail, sessionId, authKey)))
            {
                result.Code = "SaveBusinessIntegration:2";
                result.Message = "Session validation failed";
                return result;
            }

            // User validation
            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "SaveBusinessIntegration:3";
                result.Message = "User not found";
                return result;
            }

            // User business permission validation
            if (user.Permission.Business.DisableBusinessesAt != null || user.Permission.Business.EditBusinessDisabledAt != null)
            {
                result.Code = "SaveBusinessIntegration:4";
                result.Message = "User does not have permission to edit businesses";

                if (user.Permission.Business.DisableBusinessesAt != null &&
                    !string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                if (!string.IsNullOrEmpty(user.Permission.Business.EditBusinessDisableReason))
                {
                    result.Message += ": " + user.Permission.Business.EditBusinessDisableReason;
                }

                return result;
            }

            // Business ownership validation
            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "SaveBusinessIntegration:5";
                result.Message = "User does not own this business";
                return result;
            }

            // Get business data and validate
            var businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "SaveBusinessIntegration:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            // Business general permission validation
            if (businessResult.Data.Permission.DisabledFullAt != null || businessResult.Data.Permission.DisabledEditingAt != null)
            {
                result.Code = "SaveBusinessIntegration:6";
                result.Message = "Business is currently disabled";

                if (businessResult.Data.Permission.DisabledFullAt != null &&
                    !string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledEditingReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledEditingReason;
                }

                return result;
            }

            // Integration permission validation
            if (businessResult.Data.Permission.Integrations.DisabledFullAt != null)
            {
                result.Code = "SaveBusinessIntegration:7";
                result.Message = "Business does not have permission to access integrations";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Integrations.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Integrations.DisabledFullReason;
                }

                return result;
            }

            // Post type validation
            string? postType = formData["postType"].ToString();
            if (string.IsNullOrWhiteSpace(postType) || (postType != "new" && postType != "edit"))
            {
                result.Code = "SaveBusinessIntegration:8";
                result.Message = "Invalid post type";
                return result;
            }

            // Check operation-specific permissions and validate existing integration
            formData.TryGetValue("currentIntegrationId", out StringValues currentIntegrationIdValue);
            string? currentIntegrationId = currentIntegrationIdValue.ToString();

            formData.TryGetValue("currentIntegrationType", out StringValues currentIntegrationTypeValue);
            string? currentIntegrationType = currentIntegrationTypeValue.ToString();
            if (string.IsNullOrWhiteSpace(currentIntegrationType))
            {
                result.Code = "SaveBusinessIntegration:9";
                result.Message = "Missing existing integration type";
                return result;
            }

            var currentIntergationTypeData = await _integrationsManager.getIntegrationData(currentIntegrationType);
            if (!currentIntergationTypeData.Success)
            {
                result.Code = "SaveBusinessIntegration:" + currentIntergationTypeData.Code;
                result.Message = currentIntergationTypeData.Message;
                return result;
            }

            if (currentIntergationTypeData.Data.DisabledAt != null)
            {
                result.Code = "SaveBusinessIntegration:10";
                result.Message = "Current integration type is currently disabled";
                return result;
            }

            BusinessAppIntegration? businessIntegrationData = null;
            if (postType == "edit")
            {
                if (businessResult.Data.Permission.Integrations.DisabledEditingAt != null)
                {
                    result.Code = "SaveBusinessIntegration:11";
                    result.Message = "Business does not have permission to edit integrations";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Integrations.DisabledEditingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Integrations.DisabledEditingReason;
                    }

                    return result;
                }

                if (string.IsNullOrWhiteSpace(currentIntegrationId))
                {
                    result.Code = "SaveBusinessIntegration:12";
                    result.Message = "Missing existing integration id";
                    return result;
                }

                var businessIntegrationResult = await _businessManager.getBusinessIntegrationById(businessId, currentIntegrationId);
                if (!businessIntegrationResult.Success)
                {
                    result.Code = "SaveBusinessIntegration:" + businessIntegrationResult.Code;
                    result.Message = businessIntegrationResult.Message;
                    return result;
                }

                businessIntegrationData = businessIntegrationResult.Data;
            }
            else if (postType == "new")
            {
                if (businessResult.Data.Permission.Integrations.DisabledAddingAt != null)
                {
                    result.Code = "SaveBusinessIntegration:13";
                    result.Message = "Business does not have permission to add integrations";

                    if (!string.IsNullOrEmpty(businessResult.Data.Permission.Integrations.DisabledAddingReason))
                    {
                        result.Message += ": " + businessResult.Data.Permission.Integrations.DisabledAddingReason;
                    }

                    return result;
                }
            }

            // Process the integration
            var saveResult = await _businessManager.AddOrUpdateBusinessIntegration(
                businessId,
                formData,
                postType,
                currentIntergationTypeData.Data,
                businessIntegrationData,
                _integrationsManager
            );

            if (!saveResult.Success)
            {
                result.Code = "SaveBusinessIntegration:" + saveResult.Code;
                result.Message = saveResult.Message;
                return result;
            }

            result.Success = true;
            result.Data = saveResult.Data;
            return result;
        }
    }
}

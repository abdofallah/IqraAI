using IqraCore.Entities.Helpers;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessMakeCallController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessMakeCallController(UserManager userManager, BusinessManager businessManager)
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/calls/initiate")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
        public async Task<FunctionReturnResult> InitiateCalls(long businessId, [FromForm] IFormCollection formData)
        {
            var result = new FunctionReturnResult();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                return result.SetFailureResult(
                    "InitiateCalls:1",
                    "Invalid session data"
                );
            }
            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                return result.SetFailureResult(
                    "InitiateCalls:2",
                    "Session validation failed"
                );
            }
            var user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                return result.SetFailureResult(
                    "InitiateCalls:3",
                    "User not found"
                );
            }
            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCalls:4",
                    "User business editing disabled" + (string.IsNullOrWhiteSpace(user.Permission.Business.DisableBusinessesReason) ? "" : ": " + user.Permission.Business.DisableBusinessesReason)
                );
            }
            if (!user.Businesses.Contains(businessId))
            {
                return result.SetFailureResult(
                    "InitiateCalls:5",
                    "User does not own this business."
                );
            }

            var businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success || businessResult.Data == null)
            {
                return result.SetFailureResult(
                    "InitiateCalls:" + businessResult.Code,
                    businessResult.Message
                );
            }
            var business = businessResult.Data;
            if (business.Permission.DisabledFullAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCalls:7",
                    "Business is disabled for editing" + (string.IsNullOrWhiteSpace(business.Permission.DisabledFullReason) ? "" : ": " + business.Permission.DisabledFullReason)
                );
            }
            if (business.Permission.MakeCall.DisabledCallingAt != null)
            {
                return result.SetFailureResult(
                    "InitiateCalls:8",
                    "Outbound calling is disabled for this business" + (string.IsNullOrWhiteSpace(business.Permission.MakeCall.DisabledCallingReason) ? "" : ": " + business.Permission.MakeCall.DisabledCallingReason)
                );
            }

            if (!formData.TryGetValue("config", out StringValues configJsonValues) || string.IsNullOrWhiteSpace(configJsonValues.FirstOrDefault()))
            {
                return result.SetFailureResult(
                    "InitiateCalls:9",
                    "Missing 'config' data in request."
                );
            }
            string configJson = configJsonValues.First() ?? "";

            MakeCallRequestDto? callConfig;
            try
            {
                callConfig = JsonSerializer.Deserialize<MakeCallRequestDto>(configJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (callConfig == null)
                {
                    return result.SetFailureResult(
                        "InitiateCalls:10",
                        "Unable to deserialize 'config' JSON."
                    );
                }
            }
            catch (JsonException ex)
            {
                return result.SetFailureResult(
                    "InitiateCalls:11",
                    $"Invalid 'config' JSON format: {ex.Message}"
                );
            }
            IFormFile? bulkCsvFile = formData.Files.GetFile("bulk_file");

            try
            {
                var forwardResult = await _businessManager.GetMakeCallManager().ForwardCallInitiationRequestAsync(businessResult.Data, callConfig, bulkCsvFile);

                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "InitiateCalls:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "InitiateCalls:EX",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }
    }
}

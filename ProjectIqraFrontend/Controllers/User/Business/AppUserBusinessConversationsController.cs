using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Models.Business.Conversations;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;

namespace ProjectIqraFrontend.Controllers.User.Business
{
    public class AppUserBusinessConversationsController : Controller
    {
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;

        public AppUserBusinessConversationsController(
            UserManager userManager,
            BusinessManager businessManager
        )
        {
            _userManager = userManager;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/conversations/metadata")]
        public async Task<FunctionReturnResult<List<InboundConversationMetadataModel>?>> GetBusinessConversationsMetaData(long businessId)
        {
            var result = new FunctionReturnResult<List<InboundConversationMetadataModel>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinessConversationsMetaData:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetBusinessConversationsMetaData:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinessConversationsMetaData:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetBusinessConversationsMetaData:4";
                result.Message = "User does not have permission to access businesses";

                if (!string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetBusinessConversationsMetaData:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "GetBusinessConversationsMetaData:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetBusinessConversationsMetaData:6";
                result.Message = "Business is currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetBusinessConversationsMetaData:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            var conversationsResult = 

            // todo


            return result;
        }
    }
}

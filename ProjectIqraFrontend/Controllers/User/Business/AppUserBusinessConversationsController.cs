using IqraCore.Entities.Business;
using IqraCore.Entities.Conversation;
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

        [HttpGet("/app/user/business/{businessId}/conversations/inbound/metadata")]
        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetBusinessInboundConversationsMetaData(
            long businessId,
            [FromQuery] int limit = 5,
            [FromQuery] string? next = null, 
            [FromQuery] string? prev = null
        )
        {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinessInboundConversationsMetaData:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetBusinessInboundConversationsMetaData:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:4";
                result.Message = "User does not have permission to access businesses";

                if (!string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetBusinessInboundConversationsMetaData:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:6";
                result.Message = "Business is currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.Inbound.DisabledFullAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:8";
                result.Message = "Business inbound conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.Inbound.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Conversations.Inbound.DisabledFullReason;
                }

                return result;
            }

            var conversationMetaDataListResult = await _businessManager.GetConversationsManager() .GetInboundConversationsMetaDataListAsync(businessId, limit, next, prev);
            if (!conversationMetaDataListResult.Success)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:" + conversationMetaDataListResult.Code;
                result.Message = conversationMetaDataListResult.Message;
                return result;
            }

            return conversationMetaDataListResult;
        }

        [HttpGet("/app/user/business/{businessId}/conversations/outbound/metadata")]
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetBusinessOutboundConversationsMetaData(
            long businessId,
            [FromQuery] int limit = 5,
            [FromQuery] string? next = null,
            [FromQuery] string? prev = null
        )
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:4";
                result.Message = "User does not have permission to access businesses";

                if (!string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:6";
                result.Message = "Business is currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.Outbound.DisabledFullAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:8";
                result.Message = "Business outbound conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.Outbound.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.Conversations.Outbound.DisabledFullReason;
                }

                return result;
            }

            var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataListAsync(businessId, limit, next, prev);
            if (!conversationMetaDataListResult.Success)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:" + conversationMetaDataListResult.Code;
                result.Message = conversationMetaDataListResult.Message;
                return result;
            }

            return conversationMetaDataListResult;
        }

        [HttpGet("/app/user/business/{businessId}/conversations/state/{conversationSessionId}")]
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationState(long businessId, string conversationSessionId)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            string? sessionId = Request.Cookies["sessionId"];
            string? authKey = Request.Cookies["authKey"];
            string? userEmail = Request.Cookies["userEmail"];

            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(authKey) || string.IsNullOrEmpty(userEmail))
            {
                result.Code = "GetConversationState:1";
                result.Message = "Invalid session data";
                return result;
            }

            if (!await _userManager.ValidateSession(userEmail, sessionId, authKey))
            {
                result.Code = "GetConversationState:2";
                result.Message = "Session validation failed";
                return result;
            }

            UserData? user = await _userManager.GetUserByEmail(userEmail);
            if (user == null)
            {
                result.Code = "GetConversationState:3";
                result.Message = "User not found";
                return result;
            }

            if (user.Permission.Business.DisableBusinessesAt != null)
            {
                result.Code = "GetConversationState:4";
                result.Message = "User does not have permission to access businesses";

                if (!string.IsNullOrEmpty(user.Permission.Business.DisableBusinessesReason))
                {
                    result.Message += ": " + user.Permission.Business.DisableBusinessesReason;
                }

                return result;
            }

            if (!user.Businesses.Contains(businessId))
            {
                result.Code = "GetConversationState:5";
                result.Message = "User does not own this business.";
                return result;
            }

            FunctionReturnResult<BusinessData?> businessResult = await _businessManager.GetUserBusinessById(businessId, userEmail);
            if (!businessResult.Success)
            {
                result.Code = "GetConversationState:" + businessResult.Code;
                result.Message = businessResult.Message;
                return result;
            }

            if (businessResult.Data.Permission.DisabledFullAt != null)
            {
                result.Code = "GetConversationState:6";
                result.Message = "Business is currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessResult.Data.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetConversationState:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessResult.Data.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessResult.Data.Permission.DisabledFullReason;
                }

                return result;
            }

            var stateResult = await _businessManager.GetConversationsManager().GetConversationStateViewModelByIdAsync(businessId, conversationSessionId);
            if (!stateResult.Success)
            {
                result.Code = "GetConversationState:" + stateResult.Code;
                result.Message = stateResult.Message;
                return result;
            }

            return stateResult;
        }
    }
}

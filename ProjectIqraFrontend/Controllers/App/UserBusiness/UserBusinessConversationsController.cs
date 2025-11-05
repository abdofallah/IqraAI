using IqraCore.Entities.Business;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Models.Business.Conversations;
using IqraCore.Models.Business.Queues;
using IqraCore.Models.Business.Queues.Inbound;
using IqraCore.Models.Business.Queues.Outbound;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.User;
using Microsoft.AspNetCore.Mvc;
using ProjectIqraFrontend.Middlewares;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessConversationsController : Controller
    {
        private readonly UserSessionValidationHelper _userSessionValidationHelper;
        private readonly UserManager _userManager;
        private readonly BusinessManager _businessManager;
        private readonly WhiteLabelContext _whiteLabelContext;

        public UserBusinessConversationsController(
            UserSessionValidationHelper userSessionValidationHelper,
            UserManager userManager,
            BusinessManager businessManager,
            WhiteLabelContext whiteLabelContext
        )
        {
            _userSessionValidationHelper = userSessionValidationHelper;
            _userManager = userManager;
            _businessManager = businessManager;
            _whiteLabelContext = whiteLabelContext;
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

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkBusinessIsDisabled: true,
                whiteLabelContext: _whiteLabelContext
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"GetBusinessInboundConversationsMetaData:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessData.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Conversations.Inbound.DisabledFullAt != null)
            {
                result.Code = "GetBusinessInboundConversationsMetaData:8";
                result.Message = "Business inbound conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessData.Permission.Conversations.Inbound.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Conversations.Inbound.DisabledFullReason;
                }

                return result;
            }

            var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetInboundConversationsMetaDataListAsync(
                businessId,
                new GetBusinessInboundCallQueuesRequestModel()
                {
                    Limit = limit,
                    NextCursor = next,
                    PreviousCursor = prev
                }
            );
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

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkBusinessIsDisabled: true,
                whiteLabelContext: _whiteLabelContext
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"GetBusinessOutboundConversationsMetaData:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessData.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.DisabledFullReason;
                }

                return result;
            }

            if (businessData.Permission.Conversations.Outbound.DisabledFullAt != null)
            {
                result.Code = "GetBusinessOutboundConversationsMetaData:8";
                result.Message = "Business outbound conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessData.Permission.Conversations.Outbound.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.Conversations.Outbound.DisabledFullReason;
                }

                return result;
            }

            var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataListAsync(
                businessId,
                new GetBusinessOutboundCallQueuesRequestModel()
                {
                    Limit = limit,
                    NextCursor = next,
                    PreviousCursor = prev
                }
            );
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

            // Validation
            var userSessionAndBusinessValidationResult = await _userSessionValidationHelper.ValidateUserSessionAndGetUserAndBusinessAsync(
                Request,
                businessId,
                checkUserDisabled: true,
                checkUserBusinessesDisabled: true,
                checkBusinessIsDisabled: true,
                whiteLabelContext: _whiteLabelContext
            );
            if (!userSessionAndBusinessValidationResult.Success)
            {
                result.Code = $"GetConversationState:{userSessionAndBusinessValidationResult.Code}";
                result.Message = userSessionAndBusinessValidationResult.Message;
                return result;
            }
            var userData = userSessionAndBusinessValidationResult.Data!.userData!;
            var businessData = userSessionAndBusinessValidationResult.Data!.businessData!;

            if (businessData.Permission.Conversations.DisabledFullAt != null)
            {
                result.Code = "GetConversationState:7";
                result.Message = "Business conversations are currently disabled";

                if (!string.IsNullOrEmpty(businessData.Permission.Conversations.DisabledFullReason))
                {
                    result.Message += ": " + businessData.Permission.DisabledFullReason;
                }

                return result;
            }

            var stateResult = await _businessManager.GetConversationsManager().GetConversationState(businessId, conversationSessionId);
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

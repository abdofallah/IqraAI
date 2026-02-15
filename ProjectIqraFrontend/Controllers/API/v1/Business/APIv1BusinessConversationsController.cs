using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.Business.Conversations;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/conversations")]
    public class APIv1BusinessConversationsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessConversationsController(
            ISessionValidationAndPermissionHelper sessionValidationAndPermissionHelper,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = sessionValidationAndPermissionHelper;
            _businessManager = businessManager;
        }

        [HttpPost]
        public async Task<FunctionReturnResult<PaginatedResult<ConversationStateViewModel>?>> GetConversations(long businessId, [FromBody] GetBusinessConversationsRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<ConversationStateViewModel>?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.ConversationPermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.ConversationPermissions",
                            Type = BusinessModulePermissionType.Retrieving,
                        }
                    }
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversations:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetConversations:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetConversationStatesAsync(businessId, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetConversations:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetConversationState:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpGet("{sessionId}")]
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversation(long businessId, string sessionId)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            try
            {
                // API Key Validation
                var apiKeyValidaiton = await _userSessionValidationAndPermissionHelper.ValidateUserAPIAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    checkAPIKeyBusinessRestriction: true,
                    // User Permission
                    checkUserDisabled: true,
                    // User Business Permission
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    // Business Permission
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
                    // Business Module Permissions,
                    ModulePermissionsToCheck: new List<ModulePermissionCheckData>()
                    {
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.ConversationPermissions",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.ConversationPermissions",
                            Type = BusinessModulePermissionType.Retrieving,
                        }
                    }
                );
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversation:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetConversationState(businessId, sessionId);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetConversation:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetConversation:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }
    
        // TODO
        // get conversations count
    }
}

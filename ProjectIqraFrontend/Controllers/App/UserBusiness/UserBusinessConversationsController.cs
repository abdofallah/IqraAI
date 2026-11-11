using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WebSession;
using IqraCore.Entities.WhiteLabel;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.Business.Conversations;
using IqraCore.Models.Business.Queues;
using IqraCore.Models.Business.Queues.Inbound;
using IqraCore.Models.Business.Queues.Outbound;
using IqraCore.Models.Business.WebSession;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.App.Business
{
    public class UserBusinessConversationsController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly WhiteLabelContext? _whiteLabelContext;
        private readonly BusinessManager _businessManager;

        public UserBusinessConversationsController(
            ISessionValidationAndPermissionHelper userSessionValidationHelper,
            WhiteLabelContext? whiteLabelContext,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationHelper;
            _whiteLabelContext = whiteLabelContext;
            _businessManager = businessManager;
        }

        [HttpPost("/app/user/business/{businessId}/conversations/inbound/metadata")]
        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetBusinessInboundConversationsMetaData(
            long businessId,
            [FromBody] GetBusinessInboundCallQueuesRequestModel requestModel
        ) {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
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
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.Inbound",
                            Type = BusinessModulePermissionType.Full,
                        },
                        new ModulePermissionCheckData()
                        {
                            ModulePath = "Conversations.Inbound",
                            Type = BusinessModulePermissionType.Retrieving,
                        },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessInboundConversationsMetaData:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetInboundConversationsMetaDataListAsync(
                    businessId,
                    requestModel
                );
                if (!conversationMetaDataListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessInboundConversationsMetaData:{conversationMetaDataListResult.Code}",
                        conversationMetaDataListResult.Message
                    );
                }

                return result.SetSuccessResult(conversationMetaDataListResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessInboundConversationsMetaData:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/conversations/outbound/metadata")]
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetBusinessOutboundConversationsMetaData(
            long businessId,
            [FromBody] GetBusinessOutboundCallQueuesRequestModel requestModel
        ) {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
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
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Conversations.Outbound",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Conversations.Outbound",
                        Type = BusinessModulePermissionType.Retrieving,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessOutboundConversationsMetaData:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataListAsync(
                    businessId,
                    requestModel
                );
                if (!conversationMetaDataListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessOutboundConversationsMetaData:{conversationMetaDataListResult.Code}",
                        conversationMetaDataListResult.Message
                    );
                }

                return result.SetSuccessResult(conversationMetaDataListResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessOutboundConversationsMetaData:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/conversations/websession/metadata")]
        public async Task<FunctionReturnResult<PaginatedResult<WebSessionConversationMetadataModel>?>> GetBusinessWebSessionConversationsMetaData(
            long businessId,
            [FromBody] GetBusinessWebSessionsRequestModel requestModel
        ) {
            var result = new FunctionReturnResult<PaginatedResult<WebSessionConversationMetadataModel>?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
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
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Conversations.WebSession",
                        Type = BusinessModulePermissionType.Full,
                    },
                    new ModulePermissionCheckData()
                    {
                        ModulePath = "Conversations.WebSession",
                        Type = BusinessModulePermissionType.Retrieving,
                    },
                    }
                );
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessWebSessionConversationsMetaData:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var conversationMetaDataListResult = await _businessManager.GetConversationsManager().GetWebSessionsMetaDataListAsync(
                    businessId,
                    requestModel
                );
                if (!conversationMetaDataListResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetBusinessWebSessionConversationsMetaData:{conversationMetaDataListResult.Code}",
                        conversationMetaDataListResult.Message
                    );
                }

                return result.SetSuccessResult(conversationMetaDataListResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetBusinessWebSessionConversationsMetaData:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/conversations/state/{conversationSessionId}")]
        public async Task<FunctionReturnResult<ConversationStateViewModel?>> GetConversationState(long businessId, string conversationSessionId)
        {
            var result = new FunctionReturnResult<ConversationStateViewModel?>();

            try
            {
                // Validation
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
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
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversationState:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var stateResult = await _businessManager.GetConversationsManager().GetConversationState(businessId, conversationSessionId);
                if (!stateResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetConversationState:{stateResult.Code}",
                        stateResult.Message
                    );
                }

                return result.SetSuccessResult(stateResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetConversationState:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/conversations/queue/{queueId}/logs")]
        public async Task<FunctionReturnResult<List<CallQueueLogEntry>?>> GetQueueLogs(long businessId, string queueId)
        {
            var result = new FunctionReturnResult<List<CallQueueLogEntry>?>();

            try
            {
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
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
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetQueueLogs:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var logsResult = await _businessManager.GetConversationsManager().GetCallQueueLogsAsync(businessId, queueId);
                if (!logsResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetQueueLogs:{logsResult.Code}",
                        logsResult.Message
                    );
                }

                return result.SetSuccessResult(logsResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetQueueLogs:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpPost("/app/user/business/{businessId}/conversations/session/{sessionId}/logs")]
        public async Task<FunctionReturnResult<object?>> GetSessionLogs(
            long businessId,
            string sessionId,
            [FromBody] GetSessionLogsRequestModel requestModel)
        {
            var result = new FunctionReturnResult<object?>();

            try
            {
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
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
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetSessionLogs:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var logsResult = await _businessManager.GetConversationsManager().GetSessionLogsPaginatedAsync(
                    businessId,
                    sessionId,
                    requestModel.Page,
                    requestModel.PageSize,
                    requestModel.Search,
                    requestModel.LevelFilter
                );
                if (!logsResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetSessionLogs:{logsResult.Code}",
                        logsResult.Message
                    );
                }

                var data = logsResult.Data;
                return result.SetSuccessResult(new
                {
                    logs = data?.Logs ?? new List<ConversationStateLogEntry>(),
                    totalCount = data?.TotalCount ?? 0,
                    page = requestModel.Page,
                    pageSize = requestModel.PageSize
                });
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetSessionLogs:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }

        [HttpGet("/app/user/business/{businessId}/conversations/websession/{webSessionId}/logs")]
        public async Task<FunctionReturnResult<List<WebSessionLogEntry>?>> GetWebSessionLogs(long businessId, string webSessionId)
        {
            var result = new FunctionReturnResult<List<WebSessionLogEntry>?>();

            try
            {
                var userSessionAndBusinessValidationResult = await _userSessionValidationAndPermissionHelper.ValidateUserSessionAndBusinessWithPermissions(
                    Request: Request,
                    businessId: businessId,
                    whiteLabelContext: _whiteLabelContext,
                    checkUserDisabled: true,
                    checkUserBusinessesDisabled: true,
                    checkUserBusinessesEditingEnabled: true,
                    checkBusinessIsDisabled: true,
                    checkBusinessCanBeEdited: true,
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
                if (!userSessionAndBusinessValidationResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetWebSessionLogs:{userSessionAndBusinessValidationResult.Code}",
                        userSessionAndBusinessValidationResult.Message
                    );
                }

                var logsResult = await _businessManager.GetConversationsManager().GetWebSessionLogsAsync(businessId, webSessionId);
                if (!logsResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetWebSessionLogs:{logsResult.Code}",
                        logsResult.Message
                    );
                }

                return result.SetSuccessResult(logsResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebSessionLogs:EXCEPTION",
                    $"Exception: {ex.Message}"
                );
            }
        }
    }
}

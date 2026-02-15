using IqraCore.Entities.Business.ModulePermission.ENUM;
using IqraCore.Entities.Helpers;
using IqraCore.Interfaces.Validation;
using IqraCore.Models.Business.Queues;
using IqraCore.Models.Business.Queues.Inbound;
using IqraCore.Models.Business.Queues.Outbound;
using IqraInfrastructure.Managers.Business;
using Microsoft.AspNetCore.Mvc;
using IqraCore.Entities.Validation;

namespace ProjectIqraFrontend.Controllers.API.v1.Business
{
    [ApiController]
    [Route("api/v1/business/{businessId}/queues")]
    public class APIv1BusinessQueuesController : Controller
    {
        private readonly ISessionValidationAndPermissionHelper _userSessionValidationAndPermissionHelper;
        private readonly BusinessManager _businessManager;

        public APIv1BusinessQueuesController(
            ISessionValidationAndPermissionHelper userSessionValidationAndPermissionHelper,
            BusinessManager businessManager
        ) {
            _userSessionValidationAndPermissionHelper = userSessionValidationAndPermissionHelper;
            _businessManager = businessManager;
        }

        /**
         * 
         * Outbound
         * 
        **/

        [HttpPost("outbound/count")]
        public async Task<FunctionReturnResult<long?>> GetOutboundCallQueuesCount(long businessId, [FromBody] GetBusinessOutboundCallQueuesCountRequestModel modelData)
        {
            var result = new FunctionReturnResult<long?>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetOutboundCallQueuesCount:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueuesCount:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetOutboundCallQueuesCountAsync(businessId, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetOutboundCallQueuesCount:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueuesCount:EXCEPTION",
                    $"Internal Server Error: {ex.Message}"
                );
            }
        }

        [HttpPost("outbound")]
        public async Task<FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>> GetOutboundCallQueues(long businessId, [FromBody] GetBusinessOutboundCallQueuesRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<OutboundConversationMetadataModel>?>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetOutboundCallQueues:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataListAsync(businessId, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpPost("outbound/{queueId}")]
        public async Task<FunctionReturnResult<OutboundConversationMetadataModel>> GetOutboundCallQueue(long businessId, string queueId)
        {
            var result = new FunctionReturnResult<OutboundConversationMetadataModel>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetOutboundCallQueues:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetOutboundConversationsMetaDataAsync(businessId, queueId);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetOutboundCallQueues:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetOutboundCallQueues:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        /**
         * 
         * INBOUND 
         * 
        **/

        [HttpPost("inbound/count")]
        public async Task<FunctionReturnResult<long?>> GetInboundCallQueuesCount(long businessId, [FromBody] GetBusinessInboundCallQueuesCountRequestModel modelData)
        {
            var result = new FunctionReturnResult<long?>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetInboundCallQueuesCount:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetInboundCallQueuesCount:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetInboundCallQueuesCountAsync(businessId, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        $"GetInboundCallQueuesCount:{forwardResult.Code}",
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundCallQueuesCount:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpPost("inbound")]
        public async Task<FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>> GetInboundCallQueues(long businessId, [FromBody] GetBusinessInboundCallQueuesRequestModel modelData)
        {
            var result = new FunctionReturnResult<PaginatedResult<InboundConversationMetadataModel>?>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetInboundCallQueues:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Model Validation
                if (!TryValidateModel(modelData))
                {
                    return result.SetFailureResult(
                        "GetInboundCallQueues:INVALID_MODEL_DATA",
                        $"Invalid model data:\n{string.Join(", ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage))}"
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetInboundConversationsMetaDataListAsync(businessId, modelData);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetInboundCallQueues:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundCallQueues:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }

        [HttpPost("inbound/{queueId}")]
        public async Task<FunctionReturnResult<InboundConversationMetadataModel?>> GetInboundCallQueue(long businessId, string queueId)
        {
            var result = new FunctionReturnResult<InboundConversationMetadataModel?>();

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
                if (!apiKeyValidaiton.Success)
                {
                    return result.SetFailureResult(
                        $"GetInboundCallQueue:{apiKeyValidaiton.Code}",
                        apiKeyValidaiton.Message
                    );
                }

                // Forward
                var forwardResult = await _businessManager.GetConversationsManager().GetInboundConversationsMetaDataAsync(businessId, queueId);
                if (!forwardResult.Success)
                {
                    return result.SetFailureResult(
                        "GetInboundCallQueue:" + forwardResult.Code,
                        forwardResult.Message
                    );
                }

                return result.SetSuccessResult(forwardResult.Data);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundCallQueue:EXCEPTION",
                    $"Internal server error processing request: {ex.Message}"
                );
            }
        }
    }
}

using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Turn;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Repositories.Call;
using IqraInfrastructure.Repositories.Conversation;
using IqraInfrastructure.Repositories.WebSession;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Call
{
    public class CampaignActionExecutorService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<CampaignActionExecutorService> _logger;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly WebSessionRepository _webSessionRepository;
        private readonly ConversationStateRepository _conversationStateRepository;
        private readonly BusinessManager _businessManager;

        public CampaignActionExecutorService(
            ILoggerFactory loggerFactory,
            InboundCallQueueRepository inboundCallQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            WebSessionRepository webSessionRepository,
            ConversationStateRepository conversationStateRepository,
            BusinessManager businessManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CampaignActionExecutorService>();
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepo = outboundCallQueueRepository;
            _webSessionRepository = webSessionRepository;
            _conversationStateRepository = conversationStateRepository;
            _businessManager = businessManager;
        }

        // Outbound Telephony
        public async Task SendOutboundCallQueueTelephonyCampaignAction(string outboundCallQueueId, string logMessage)
        {
            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueByIdAsync(outboundCallQueueId);
            if (outboundCallQueueData == null)
            {
                _logger.LogError("Unable to find outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueId);
                return;
            }

            if (outboundCallQueueData.Status == CallQueueStatusEnum.Queued ||
                outboundCallQueueData.Status == CallQueueStatusEnum.ProcessingProxy ||
                outboundCallQueueData.Status == CallQueueStatusEnum.ProcessedProxy ||
                outboundCallQueueData.Status == CallQueueStatusEnum.ProcessingBackend
            ) {
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundCallQueueTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business {businessId} for outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueData.BusinessId, outboundCallQueueId);

                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find business {outboundCallQueueData.BusinessId} for outbound call queue {outboundCallQueueId} to send campaign action: [{businessDataResult.Code}] {businessDataResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundCallQueueTelephonyCampaignAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app for outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueId);

                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find business app for outbound call queue {outboundCallQueueId} to send campaign action: [{businessAppResult.Code}] {businessAppResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find call queue campaign to find and send campaign action if any. [{callQueueTelephonyCampaignResult.Code}] {callQueueTelephonyCampaignResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (outboundCallQueueData.Status == CallQueueStatusEnum.Failed || outboundCallQueueData.Status == CallQueueStatusEnum.Canceled || outboundCallQueueData.Status == CallQueueStatusEnum.Expired)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallInitiationFailureTool.ToolId)) return;

                var callInitiationFailureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallInitiationFailureTool.ToolId);
                if (callInitiationFailureToolData == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to find call queue campaign call initiation failure tool to find and send campaign action.",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callFailureArgumentsResult = GetTelephonyCampaignCallInitiationFailureArguements(outboundCallQueueData, logMessage);
                if (!callFailureArgumentsResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to get call queue campaign call initiation failure tool arguements. [{callFailureArgumentsResult.Code}] {callFailureArgumentsResult.Message} ",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var callFailureArguments = callFailureArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = telephonyCampaign.Actions.CallInitiationFailureTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callFailureArguments
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    callInitiationFailureToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to execute call queue campaign call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                else
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Call queue campaign call initiation failure tool response:\n```{executeActionToolResult.Data}```",
                            Type = CallQueueLogTypeEnum.Information
                        }
                    );
                }

                return;
            }
            else if (outboundCallQueueData.Status == CallQueueStatusEnum.ProcessedBackend)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallAnsweredTool.ToolId)) return;

                var conversationState = await _conversationStateRepository.GetByIdAsync(outboundCallQueueData.SessionId!);
                if (conversationState == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to find call queue campaign conversation session to send send campaign action.",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );
                }

                var callAnsweredToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallAnsweredTool.ToolId);
                if (callAnsweredToolData == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to find call queue campaign call answered tool to find and send campaign action.",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callAnsweredArgumentsResult = GetTelephonyCampaignCallAnsweredArguements(outboundCallQueueData, conversationState);
                if (!callAnsweredArgumentsResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to get call queue campaign call answered tool arguements. [{callAnsweredArgumentsResult.Code}] {callAnsweredArgumentsResult.Message} ",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var callAnsweredArguments = callAnsweredArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = telephonyCampaign.Actions.CallAnsweredTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callAnsweredArgumentsResult.Data!
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    callAnsweredToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to execute call queue campaign call answered tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                else
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Call queue campaign call answered tool response:\n```{executeActionToolResult.Message}```",
                            Type = CallQueueLogTypeEnum.Information
                        }
                    );
                }

                return;
            }
        }
        public async Task SendOutboundConversationSessionEndedTelephonyCampaignAction(string outboundConversationSessionId, string reason)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(outboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for outbound conversation session id {OutboundConversationSessionId} to run action for reason {Reason}.", outboundConversationSessionId, reason);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Ended)
            {
                _logger.LogError("Outbound conversation session id {OutboundConversationSessionId} invalid status (not ended) {Status} to run action for reason {Reason}.", outboundConversationSessionId, converationStateData.Status.ToString(), reason);

                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Outbound conversation session id {outboundConversationSessionId} invalid status (not ended) to run action for reason {reason}.",
                    }
                );

                return;
            }

            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueBySessionIdAsync(outboundConversationSessionId);
            if (outboundCallQueueData == null)
            {
                _logger.LogError("Unable to find outbound call queue data for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId);

                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to find outbound call queue data for outbound conversation session id {outboundConversationSessionId}.",
                    }
                );

                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for outbound call queue id {OutboundCallQueueId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, outboundCallQueueData.Id);

                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to find business data for outbound call queue id {outboundCallQueueData.Id} to send session telephony campaign action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                _logger.LogError("Unable to find telephony campaign data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to find telephony campaign data to send session telephony campaign action if any.",
                    }
                );
                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallEndedTool.ToolId)) return;

            var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallEndedTool.ToolId!);
            if (conversationEndedToolData == null)
            {
                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to find conversation ended tool data with id {telephonyCampaign.Actions.CallEndedTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send conversation end action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var callEndedArgumentsResult = GetTelephonyCampaignCallEndArguements(outboundCallQueueData, converationStateData);
            if (!callEndedArgumentsResult.Success)
            {
                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to get call failure arguments for outbound conversation session id {outboundConversationSessionId} to send conversation end action.",
                    }
                );

                return;
            }
            var callEndedArguments = callEndedArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = telephonyCampaign.Actions.CallEndedTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        callEndedArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationEndedToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Error,
                        Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );

                return;
            }
            else
            {
                await _conversationStateRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationLogEntry
                    {
                        Level = ConversationLogLevel.Information,
                        Message = $"Telephony campaign call ended tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }

            return;
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(OutboundCallQueueData callQueueData, string logMessage)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // Call Queue Data from the base class
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },

                    // OutboundCallQueueData specific fields
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", callQueueData.CallingNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },
            
                    // Conversation related
                    { "conversation_id", callQueueData.SessionId }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements:EXCEPTION",
                    $"Error getting telephony campaign call initiation/declined/missed arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetTelephonyCampaignCallInitiationFailureArguements(OutboundCallQueueData callQueueData, string logMessage)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // Call Queue Data from the base class
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_session_id", callQueueData.SessionId },

                    // OutboundCallQueueData specific fields
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", callQueueData.CallingNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },
            
                    // The specific error message for this failure
                    { "call_queue_initiation_error", logMessage }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignCallInitiationFailureArguements:EXCEPTION",
                    $"Error getting telephony campaign call initiation failure arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetTelephonyCampaignCallAnsweredArguements(OutboundCallQueueData callQueueData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Call Queue Data ---
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", callQueueData.CallingNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignCallAnsweredArguements:EXCEPTION",
                    $"Error getting telephony campaign call answered arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetTelephonyCampaignCallEndArguements(OutboundCallQueueData callQueueData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Call Queue Data ---
                    { "call_queue_id", callQueueData.Id },
                    { "call_queue_created_at", callQueueData.CreatedAt },
                    { "call_queue_enqueued_at", callQueueData.EnqueuedAt },
                    { "call_queue_processing_started_at", callQueueData.ProcessingStartedAt },
                    { "call_queue_completed_at", callQueueData.CompletedAt },
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", callQueueData.CallingNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignCallEndArguements:EXCEPTION",
                    $"Error getting telephony campaign call end arguements: {ex.Message}"
                );
            }
        }

        // Inbound Telephony
        public async Task SendInboundConversationSessionTelephonyCampaignAction(string inboundConversationSessionId)
        {

        }

        // Web Session
        public async Task SendWebSessionCampaignAction(string webSessionId)
        {
            // initiation failure, initiated
        }
        public async Task SendWebConversationSessionCampaignAction(string webConversationSessionId)
        {
            // ended
        }

        // Common Helpers
        private string SimplifyConversationTurns(List<ConversationTurn> turns)
        {
            var stringResult = "";

            foreach (var turn in turns)
            {
                if (turn.Type == ConversationTurnType.System)
                {
                    stringResult += $"[{turn.CreatedAt.ToString("G")}] SYSTEM ({turn.SystemInput!.Type})\n";
                }
                else if (turn.Type == ConversationTurnType.User)
                {
                    stringResult += $"[{turn.UserInput!.StartedSpeakingAt.ToString("G")}] USER: {turn.UserInput.TranscribedText}\n";
                }

                if (turn.Response.Type == ConversationTurnAgentResponseType.Speech)
                {
                    stringResult += $"[{(turn.Response.LLMStreamingStartedAt ?? turn.Response.SpokenSegments[0].StartedPlayingAt).ToString("G")}] ASSISTANT: {turn.Response.SpokenSegments.Select(d => $"{d.Text} ")}";
                }
                else if (turn.Response.Type == ConversationTurnAgentResponseType.CustomTool || turn.Response.Type == ConversationTurnAgentResponseType.SystemTool)
                {
                    stringResult += $"[{(turn.Response.LLMStreamingStartedAt ?? turn.Response.LLMStreamingCompletedAt ?? turn.Response.LLMProcessStartedAt)?.ToString("G")}] ASSISTANT: {turn.Response.ToolExecution!.RawLLMInput}";
                }
            }

            return stringResult.TrimEnd();
        }
    }
}

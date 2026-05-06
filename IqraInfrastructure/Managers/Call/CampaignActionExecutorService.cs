using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Conversation;
using IqraCore.Entities.Conversation.Enum;
using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Conversation.Logs.Enums;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.WebSession;
using IqraInfrastructure.Helpers;
using IqraInfrastructure.Helpers.Conversation;
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
        private readonly ConversationStateLogsRepository _conversationStateLogsRepository;
        private readonly BusinessManager _businessManager;

        public CampaignActionExecutorService(
            ILoggerFactory loggerFactory,
            InboundCallQueueRepository inboundCallQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            WebSessionRepository webSessionRepository,
            ConversationStateRepository conversationStateRepository,
            ConversationStateLogsRepository conversationStateLogsRepository,
            BusinessManager businessManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CampaignActionExecutorService>();
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepo = outboundCallQueueRepository;
            _webSessionRepository = webSessionRepository;
            _conversationStateRepository = conversationStateRepository;
            _conversationStateLogsRepository = conversationStateLogsRepository;
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
                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Outbound call queue {outboundCallQueueId} invalid status {outboundCallQueueData.Status.ToString()} to send campaign action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundCallQueueTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLogEntry
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
                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLogEntry
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
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find call queue campaign to find and send campaign action if any. [{callQueueTelephonyCampaignResult.Code}] {callQueueTelephonyCampaignResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            // Inititation Failure
            if (
                outboundCallQueueData.Status == CallQueueStatusEnum.Failed ||
                outboundCallQueueData.Status == CallQueueStatusEnum.Canceled ||
                outboundCallQueueData.Status == CallQueueStatusEnum.Expired
            ) {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallInitiationFailureTool.ToolId)) return;

                var callInitiationFailureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallInitiationFailureTool.ToolId);
                if (callInitiationFailureToolData == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
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
                        new CallQueueLogEntry
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
                        new CallQueueLogEntry
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
                        new CallQueueLogEntry
                        {
                            Message = $"Call queue campaign call initiation failure tool response:\n```{executeActionToolResult.Data}```",
                            Type = CallQueueLogTypeEnum.Information
                        }
                    );
                }

                return;
            }
            // Initiated
            else if (outboundCallQueueData.Status == CallQueueStatusEnum.ProcessedBackend)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallInitiatedTool.ToolId)) return;

                var conversationState = await _conversationStateRepository.GetByIdAsync(outboundCallQueueData.SessionId!);
                if (conversationState == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
                        {
                            Message = $"Unable to find call queue campaign conversation session to send call initiated campaign action.",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );
                }

                var callInitiatedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallInitiatedTool.ToolId);
                if (callInitiatedToolData == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
                        {
                            Message = $"Unable to find call queue campaign call initiated tool to find and send call initiated campaign action.",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callInitiatedArgumentsResult = GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(outboundCallQueueData);
                if (!callInitiatedArgumentsResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
                        {
                            Message = $"Unable to get call queue campaign call initiated tool arguements. [{callInitiatedArgumentsResult.Code}] {callInitiatedArgumentsResult.Message} ",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var callInitiatedArguments = callInitiatedArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = telephonyCampaign.Actions.CallInitiatedTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callInitiatedArgumentsResult.Data!
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    callInitiatedToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
                        {
                            Message = $"Unable to execute call queue campaign call initiated tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                else
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        outboundCallQueueData.Id,
                        new CallQueueLogEntry
                        {
                            Message = $"Call queue campaign call initiated tool response:\n```{executeActionToolResult.Message}```",
                            Type = CallQueueLogTypeEnum.Information
                        }
                    );
                }

                return;
            }
            else
            {
                await _outboundCallQueueRepo.AddCallLogAsync(
                    outboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to send call queue campaign call initiated action. Call queue status is {outboundCallQueueData.Status}.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
        }
        public async Task SendOutboundConversationSessionAnsweredTelephonyCampaignAction(string outboundConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(outboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for outbound conversation session id {OutboundConversationSessionId} to run answered action.", outboundConversationSessionId);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Active)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Outbound conversation session id {outboundConversationSessionId} invalid status (not active) {converationStateData.Status.ToString()} to run answered action.",
                    }
                );

                return;
            }

            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (outboundCallQueueData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find outbound call queue data for outbound conversation session id {outboundConversationSessionId} to run answered action.",
                    }
                );

                return;
            }

            if (outboundCallQueueData.Status != CallQueueStatusEnum.ProcessingBackend)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Outbound call queue id {outboundCallQueueData.Id} invalid status (not processing backend) {outboundCallQueueData.Status.ToString()} to run answered action.",
                    }
                );
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for outbound call queue id {OutboundCallQueueId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, outboundCallQueueData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for outbound call queue id {outboundCallQueueData.Id} to send session telephony campaign answered action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign answered action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                _logger.LogError("Unable to find telephony campaign data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find telephony campaign data to send session telephony campaign answered action.",
                    }
                );
                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallAnsweredTool.ToolId)) return;

            var conversationAnsweredToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallAnsweredTool.ToolId!);
            if (conversationAnsweredToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find conversation answered tool data with id {telephonyCampaign.Actions.CallAnsweredTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send conversation answered action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var callAnsweredArgumentsResult = GetTelephonyCampaignCallAnsweredArguements(outboundCallQueueData, converationStateData);
            if (!callAnsweredArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get call answered arguments for outbound conversation session id {outboundConversationSessionId} to send conversation answered action: [{callAnsweredArgumentsResult.Code}] {callAnsweredArgumentsResult.Message}.",
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
                        callAnsweredArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationAnsweredToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute conversation answered tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );

                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Telephony campaign call answered tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
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

            if (converationStateData.Status != ConversationSessionState.Ended && converationStateData.Status != ConversationSessionState.Error)
            {
                _logger.LogError("Outbound conversation session id {OutboundConversationSessionId} invalid status (not ended/error) {Status} to run action for reason {Reason}.", outboundConversationSessionId, converationStateData.Status.ToString(), reason);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Outbound conversation session id {outboundConversationSessionId} invalid status {converationStateData.Status.ToString()} to run action if any for reason {reason}.",
                    }
                );

                return;
            }

            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (outboundCallQueueData == null)
            {
                _logger.LogError("Unable to find outbound call queue data for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find outbound call queue data for outbound conversation session id {outboundConversationSessionId} to run action if any for reason {reason}.",
                    }
                );

                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for outbound call queue id {OutboundCallQueueId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, outboundCallQueueData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for outbound call queue id {outboundCallQueueData.Id} to send session telephony campaign action if any for reason {reason}.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundConversationSessionTelephonyCampaignAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign action if any for reason {reason}.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                _logger.LogError("Unable to find telephony campaign data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find telephony campaign data to send session telephony campaign action if any for reason {reason}.",
                    }
                );
                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (
                converationStateData.EndType == ConversationSessionEndType.UserEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.AgentEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.UserSilenceTimeoutReached ||
                converationStateData.EndType == ConversationSessionEndType.MaxConversationDurationReached ||
                converationStateData.EndType == ConversationSessionEndType.VoicemailDetected ||
                converationStateData.EndType == ConversationSessionEndType.MidSessionFailure
            ) {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallEndedTool.ToolId)) return;

                var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallEndedTool.ToolId!);
                if (conversationEndedToolData == null)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
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
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to get call end arguments for outbound conversation session id {outboundConversationSessionId} to send conversation end action: [{callEndedArgumentsResult.Code}] {callEndedArgumentsResult.Message}.",
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
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        }
                    );

                    return;
                }
                else
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Information,
                            Message = $"Telephony campaign call ended tool response:\n```{executeActionToolResult.Data}```",
                        }
                    );
                }

                return;
            }
            else if (converationStateData.EndType == ConversationSessionEndType.UserDeclinedOrBusy)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallDeclinedTool.ToolId)) return;

                var conversationDeclinedOrBusyToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallDeclinedTool.ToolId!);
                if (conversationDeclinedOrBusyToolData == null)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to find conversation declined or busy tool data with id {telephonyCampaign.Actions.CallDeclinedTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send conversation end (declined or busy) action.",
                        }
                    );
                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callDeclinedOrBusyArgumentsResult = GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(outboundCallQueueData);
                if (!callDeclinedOrBusyArgumentsResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to get call declined or busy arguments for outbound conversation session id {outboundConversationSessionId} to send conversation end (declined or busy) action: [{callDeclinedOrBusyArgumentsResult.Code}] {callDeclinedOrBusyArgumentsResult.Message}.",
                        }
                    );

                    return;
                }
                var callDeclinedOrBusyArguments = callDeclinedOrBusyArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = telephonyCampaign.Actions.CallDeclinedTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callDeclinedOrBusyArguments
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    conversationDeclinedOrBusyToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to execute conversation declined or busy tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        }
                    );

                    return;
                }
                else
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Information,
                            Message = $"Telephony campaign call declined or busy tool response:\n```{executeActionToolResult.Data}```",
                        }
                    );
                }

                return;
            }
            else if (converationStateData.EndType == ConversationSessionEndType.UserNoAnswer)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallMissedTool.ToolId)) return;

                var conversationMissedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallMissedTool.ToolId!);
                if (conversationMissedToolData == null)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to find conversation missed tool data with id {telephonyCampaign.Actions.CallMissedTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send conversation end (missed) action.",
                        }
                    );
                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callMissedArgumentsResult = GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(outboundCallQueueData);
                if (!callMissedArgumentsResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to get call missed arguments for outbound conversation session id {outboundConversationSessionId} to send conversation end (missed) action: [{callMissedArgumentsResult.Code}] {callMissedArgumentsResult.Message}",
                        }
                    );

                    return;
                }
                var callMissedArguments = callMissedArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = telephonyCampaign.Actions.CallMissedTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callMissedArguments
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    conversationMissedToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to execute conversation missed tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        }
                    );

                    return;
                }
                else
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        outboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Information,
                            Message = $"Telephony campaign call missed tool response:\n```{executeActionToolResult.Data}```",
                        }
                    );
                }

                return;
            }
        }
        public async Task SendOutboundConversationSessionPostAnalysisCampaignAction(string outboundConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(outboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for outbound conversation session id {OutboundConversationSessionId} to run post analysis action.", outboundConversationSessionId);
                return;
            }

            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (outboundCallQueueData == null)
            {
                _logger.LogError("Unable to find outbound call queue data for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find outbound call queue data for outbound conversation session id {outboundConversationSessionId} to run post analysis action.",
                    }
                );
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundConversationSessionPostAnalysisAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for outbound call queue id {OutboundCallQueueId} for outbound conversation session id {OutboundConversationSessionId}.", outboundCallQueueData.Id, outboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for outbound call queue id {outboundCallQueueData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundConversationSessionPostAnalysisAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", businessData.Id, outboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                _logger.LogError("Unable to find telephony campaign data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", businessData.Id, outboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find telephony campaign data to send session post analysis action.",
                    }
                );
                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (string.IsNullOrEmpty(telephonyCampaign.Actions.ConversationPostAnalysisTool.ToolId)) return;

            var conversationPostAnalysisToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.ConversationPostAnalysisTool.ToolId!);
            if (conversationPostAnalysisToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find conversation post analysis tool data with id {telephonyCampaign.Actions.ConversationPostAnalysisTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send post analysis action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var postAnalysisArgumentsResult = GetTelephonyCampaignPostAnalysisArguements(outboundCallQueueData, converationStateData);
            if (!postAnalysisArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get post analysis arguments for outbound conversation session id {outboundConversationSessionId} to send post analysis action: [{postAnalysisArgumentsResult.Code}] {postAnalysisArgumentsResult.Message}.",
                    }
                );
                return;
            }
            var postAnalysisArguments = postAnalysisArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = telephonyCampaign.Actions.ConversationPostAnalysisTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        postAnalysisArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationPostAnalysisToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute conversation post analysis tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );
                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Telephony campaign post analysis tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }
        }

        private FunctionReturnResult<Dictionary<string, object?>?> GetTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(OutboundCallQueueData callQueueData)
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
                    { "call_queue_status", (int)callQueueData.Status },

                    // OutboundCallQueueData specific fields
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", (int)callQueueData.CallingNumberProvider },
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
                    { "call_queue_status", callQueueData.Status },
                    { "call_queue_session_id", callQueueData.SessionId },

                    // OutboundCallQueueData specific fields
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", (int)callQueueData.CallingNumberProvider },
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", (int)callQueueData.CallingNumberProvider },
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", (int)callQueueData.CallingNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
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
        private FunctionReturnResult<Dictionary<string, object?>> GetTelephonyCampaignPostAnalysisArguements(OutboundCallQueueData callQueueData, ConversationState conversationStateData)
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_campaign_id", callQueueData.CampaignId },
                    { "call_queue_calling_number_id", callQueueData.CallingNumberId },
                    { "call_queue_calling_number_provider", (int)callQueueData.CallingNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_recipient_number", callQueueData.RecipientNumber },
                    { "call_queue_scheduled_for_date_time", callQueueData.ScheduledForDateTime },
                    { "call_queue_dynamic_variables", callQueueData.DynamicVariables },
                    { "call_queue_metadata", callQueueData.Metadata },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) },

                    // --- Post Analysis Data ---
                    { "post_analysis_template_id", conversationStateData.PostAnalysis?.PostAnalysisId },
                    { "post_analysis_status_type", (int?)conversationStateData.PostAnalysis?.Status },
                    { "post_analysis_summary_data", conversationStateData.PostAnalysis?.SummaryData },
                    { "post_analysis_tagging_data", conversationStateData.PostAnalysis?.TagsData },
                    { "post_analysis_extraction_data", conversationStateData.PostAnalysis?.ExtractedFieldsData },
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetTelephonyCampaignPostAnalysisArguements:EXCEPTION",
                    $"Error getting telephony campaign post analysis arguements: {ex.Message}"
                );
            }
        }


        // Inbound Telephony
        public async Task SendInboundCallQueueRingingAction(string inboundCallQueueId)
        {
            var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(inboundCallQueueId);
            if (inboundCallQueueData == null)
            {
                _logger.LogError("Unable to find inbound call queue {InboundCallQueueId} to send ringing action.", inboundCallQueueId);
                return;
            }

            if (inboundCallQueueData.Status != CallQueueStatusEnum.ProcessingProxy)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound call queue {inboundCallQueueId} invalid status {inboundCallQueueData.Status.ToString()} to send ringing action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundCallQueueRingingAction");
            if (!businessDataResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business {inboundCallQueueData.BusinessId} for inbound call queue {inboundCallQueueId} to send ringing action: [{businessDataResult.Code}] {businessDataResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundCallQueueRingingAction");
            if (!businessAppResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business app for inbound call queue {inboundCallQueueId} to send ringing action: [{businessAppResult.Code}] {businessAppResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(inboundCallQueueData.RouteId))
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound call queue {inboundCallQueueId} does not have a valid RouteId to find telephony route.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            var businessRoute = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            if (businessRoute == null)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business route (RouteId: {inboundCallQueueData.RouteId}) to send ringing action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(businessRoute.Actions.RingingTool.ToolId)) return;

            var conversationState = await _conversationStateRepository.GetByIdAsync(inboundCallQueueData.SessionId!);
            if (conversationState == null)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find conversation session to send ringing action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
            }

            var ringingToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, businessRoute.Actions.RingingTool.ToolId);
            if (ringingToolData == null)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find inbound route ringing tool to execute.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var ringingArgumentsResult = GetInboundTelephonyCampaignCallRingingArguements(inboundCallQueueData);
            if (!ringingArgumentsResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to get inbound route ringing tool arguments. [{ringingArgumentsResult.Code}] {ringingArgumentsResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var ringingArguments = ringingArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = businessRoute.Actions.RingingTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        ringingArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                ringingToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to execute inbound route ringing tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            else
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound route ringing tool response:\n```{executeActionToolResult.Data}```",
                        Type = CallQueueLogTypeEnum.Information
                    }
                );
            }
        }
        public async Task SendInboundCallQueueInitiationFailureAction(string inboundCallQueueId, string logMessage)
        {
            var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(inboundCallQueueId);
            if (inboundCallQueueData == null)
            {
                _logger.LogError("Unable to find inbound call queue {InboundCallQueueId} to send initiation failure action.", inboundCallQueueId);
                return;
            }

            if (inboundCallQueueData.Status != CallQueueStatusEnum.Failed &&
                inboundCallQueueData.Status != CallQueueStatusEnum.Canceled &&
                inboundCallQueueData.Status != CallQueueStatusEnum.Expired)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound call queue {inboundCallQueueId} invalid status {inboundCallQueueData.Status.ToString()} to send initiation failure action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundCallQueueInitiationFailureAction");
            if (!businessDataResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business {inboundCallQueueData.BusinessId} for inbound call queue {inboundCallQueueId} to send initiation failure action: [{businessDataResult.Code}] {businessDataResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundCallQueueInitiationFailureAction");
            if (!businessAppResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business app for inbound call queue {inboundCallQueueId} to send initiation failure action: [{businessAppResult.Code}] {businessAppResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(inboundCallQueueData.RouteId))
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound call queue {inboundCallQueueId} does not have a valid RouteId to find telephony route.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            var businessRoute = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            if (businessRoute == null)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find business route (RouteId: {inboundCallQueueData.RouteId}) to send initiation failure action.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(businessRoute.Actions.CallInitiationFailureTool.ToolId)) return;

            var callInitiationFailureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, businessRoute.Actions.CallInitiationFailureTool.ToolId);
            if (callInitiationFailureToolData == null)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to find inbound route call initiation failure tool to execute.",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var callFailureArgumentsResult = GetInboundTelephonyCampaignCallInitiationFailureArguements(inboundCallQueueData, logMessage);
            if (!callFailureArgumentsResult.Success)
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to get inbound route call initiation failure tool arguments. [{callFailureArgumentsResult.Code}] {callFailureArgumentsResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            var callFailureArguments = callFailureArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = businessRoute.Actions.CallInitiationFailureTool.Arguments;
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
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Unable to execute inbound route call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );
                return;
            }
            else
            {
                await _inboundCallQueueRepository.AddCallLogAsync(
                    inboundCallQueueData.Id,
                    new CallQueueLogEntry
                    {
                        Message = $"Inbound route call initiation failure tool response:\n```{executeActionToolResult.Data}```",
                        Type = CallQueueLogTypeEnum.Information
                    }
                );
            }
        }      
        public async Task SendInboundConversationSessionPickedTelephonyCampaignAction(string inboundConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(inboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for inbound conversation session id {InboundConversationSessionId} to run picked action.", inboundConversationSessionId);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Active)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Inbound conversation session id {inboundConversationSessionId} invalid status (not active) {converationStateData.Status.ToString()} to run picked action.",
                    }
                );
                return;
            }

            var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (inboundCallQueueData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find inbound call queue data for inbound conversation session id {inboundConversationSessionId} to run picked action.",
                    }
                );
                return;
            }

            if (inboundCallQueueData.Status != CallQueueStatusEnum.ProcessingBackend)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Inbound call queue id {inboundCallQueueData.Id} invalid status (not processing backend) {inboundCallQueueData.Status.ToString()} to run picked action.",
                    }
                );
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundConversationSessionPickedAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for inbound call queue id {InboundCallQueueId} for inbound conversation session id {InboundConversationSessionId}.", inboundCallQueueData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for inbound call queue id {inboundCallQueueData.Id} to send session picked action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundConversationSessionPickedAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session picked action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            var businessRoute = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            if (businessRoute == null)
            {
                _logger.LogError("Unable to find business route data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business route data to send session picked action.",
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(businessRoute.Actions.CallPickedTool.ToolId)) return;

            var callPickedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, businessRoute.Actions.CallPickedTool.ToolId!);
            if (callPickedToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find call picked tool data with id {businessRoute.Actions.CallPickedTool.ToolId} for inbound conversation session id {inboundConversationSessionId} to send conversation picked action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var callPickedArgumentsResult = GetInboundTelephonyCampaignCallPickedArguements(inboundCallQueueData, converationStateData);
            if (!callPickedArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get call picked arguments for inbound conversation session id {inboundConversationSessionId} to send conversation picked action: [{callPickedArgumentsResult.Code}] {callPickedArgumentsResult.Message}.",
                    }
                );
                return;
            }
            var callPickedArguments = callPickedArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = businessRoute.Actions.CallPickedTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        callPickedArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                callPickedToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute call picked tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );
                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Inbound route call picked tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }
        }
        public async Task SendInboundConversationSessionEndedTelephonyCampaignAction(string inboundConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(inboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for inbound conversation session id {InboundConversationSessionId} to run action.", inboundConversationSessionId);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Ended && converationStateData.Status != ConversationSessionState.Error)
            {
                _logger.LogError("Inbound conversation session id {InboundConversationSessionId} invalid status (not ended/error) {Status} to run action.", inboundConversationSessionId, converationStateData.Status.ToString());

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Inbound conversation session id {inboundConversationSessionId} invalid status {converationStateData.Status.ToString()} to run action.",
                    }
                );
                return;
            }

            var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (inboundCallQueueData == null)
            {
                _logger.LogError("Unable to find inbound call queue data for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find inbound call queue data for inbound conversation session id {inboundConversationSessionId} to run action.",
                    }
                );
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundConversationSessionEndedAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for inbound call queue id {InboundCallQueueId} for inbound conversation session id {InboundConversationSessionId}.", inboundCallQueueData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for inbound call queue id {inboundCallQueueData.Id} to send session ended action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundConversationSessionEndedAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session ended action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            var businessRoute = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            if (businessRoute == null)
            {
                _logger.LogError("Unable to find business route data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business route data to send session ended action.",
                    }
                );
                return;
            }

            if (
                converationStateData.EndType == ConversationSessionEndType.UserEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.AgentEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.UserSilenceTimeoutReached ||
                converationStateData.EndType == ConversationSessionEndType.MaxConversationDurationReached ||
                converationStateData.EndType == ConversationSessionEndType.MidSessionFailure
            )
            {
                if (string.IsNullOrEmpty(businessRoute.Actions.CallEndedTool.ToolId)) return;

                var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, businessRoute.Actions.CallEndedTool.ToolId!);
                if (conversationEndedToolData == null)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        inboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to find conversation ended tool data with id {businessRoute.Actions.CallEndedTool.ToolId} for inbound conversation session id {inboundConversationSessionId} to send conversation end action.",
                        }
                    );
                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callEndedArgumentsResult = GetInboundTelephonyCampaignCallEndArguements(inboundCallQueueData, converationStateData);
                if (!callEndedArgumentsResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        inboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to get call end arguments for inbound conversation session id {inboundConversationSessionId} to send conversation end action: [{callEndedArgumentsResult.Code}] {callEndedArgumentsResult.Message}.",
                        }
                    );
                    return;
                }
                var callEndedArguments = callEndedArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = businessRoute.Actions.CallEndedTool.Arguments;
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
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        inboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        }
                    );
                    return;
                }
                else
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        inboundConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Information,
                            Message = $"Inbound route call ended tool response:\n```{executeActionToolResult.Data}```",
                        }
                    );
                }
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Conversation session id {inboundConversationSessionId} is not in a state where a call ended action can be sent. End Type: {converationStateData.EndType}.",
                    }
                );
            }
        }
        public async Task SendInboundConversationSessionPostAnalysisCampaignAction(string inboundConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(inboundConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for inbound conversation session id {InboundConversationSessionId} to run post analysis action.", inboundConversationSessionId);
                return;
            }

            var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(converationStateData.QueueId!);
            if (inboundCallQueueData == null)
            {
                _logger.LogError("Unable to find inbound call queue data for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find inbound call queue data for inbound conversation session id {inboundConversationSessionId} to run post analysis action.",
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundConversationSessionPostAnalysisAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for inbound call queue id {InboundCallQueueId} for inbound conversation session id {InboundConversationSessionId}.", inboundCallQueueData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for inbound call queue id {inboundCallQueueData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundConversationSessionPostAnalysisAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var businessRoute = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId!);
            if (businessRoute == null)
            {
                _logger.LogError("Unable to find business route data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", businessData.Id, inboundConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business route data to send session post analysis action.",
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(businessRoute.Actions.ConversationPostAnalysisTool.ToolId)) return;

            var conversationPostAnalysisToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, businessRoute.Actions.ConversationPostAnalysisTool.ToolId!);
            if (conversationPostAnalysisToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find conversation post analysis tool data with id {businessRoute.Actions.ConversationPostAnalysisTool.ToolId} for inbound conversation session id {inboundConversationSessionId} to send post analysis action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var postAnalysisArgumentsResult = GetInboundTelephonyCampaignPostAnalysisArguements(inboundCallQueueData, converationStateData);
            if (!postAnalysisArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get post analysis arguments for inbound conversation session id {inboundConversationSessionId} to send post analysis action: [{postAnalysisArgumentsResult.Code}] {postAnalysisArgumentsResult.Message}.",
                    }
                );
                return;
            }
            var postAnalysisArguments = postAnalysisArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = businessRoute.Actions.ConversationPostAnalysisTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        postAnalysisArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationPostAnalysisToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute conversation post analysis tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );
                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    inboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Inbound route post analysis tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }
        }

        private FunctionReturnResult<Dictionary<string, object?>?> GetInboundTelephonyCampaignCallRingingArguements(InboundCallQueueData callQueueData)
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
                    { "call_queue_status", (int)callQueueData.Status },

                    // InboundCallQueueData specific fields
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", (int)callQueueData.RouteNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements:EXCEPTION",
                    $"Error getting telephony campaign call initiation/declined/missed arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetInboundTelephonyCampaignCallInitiationFailureArguements(InboundCallQueueData callQueueData, string logMessage)
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_session_id", callQueueData.SessionId },

                    // InboundCallQueueData specific fields
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", (int)callQueueData.RouteNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },
            
                    // The specific error message for this failure
                    { "call_queue_initiation_error", logMessage }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundTelephonyCampaignCallInitiationFailureArguements:EXCEPTION",
                    $"Error getting telephony campaign call initiation failure arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetInboundTelephonyCampaignCallPickedArguements(InboundCallQueueData callQueueData, ConversationState conversationStateData)
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", (int)callQueueData.RouteNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundTelephonyCampaignCallAnsweredArguements:EXCEPTION",
                    $"Error getting telephony campaign call answered arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetInboundTelephonyCampaignCallEndArguements(InboundCallQueueData callQueueData, ConversationState conversationStateData)
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", (int)callQueueData.RouteNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundTelephonyCampaignCallEndArguements:EXCEPTION",
                    $"Error getting telephony campaign call end arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetInboundTelephonyCampaignPostAnalysisArguements(InboundCallQueueData callQueueData, ConversationState conversationStateData)
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
                    { "call_queue_status", (int)callQueueData.Status },
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", (int)callQueueData.RouteNumberProvider },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) },

                    // --- Post Analysis Data ---
                    { "post_analysis_template_id", conversationStateData.PostAnalysis?.PostAnalysisId },
                    { "post_analysis_status_type", (int?)conversationStateData.PostAnalysis?.Status },
                    { "post_analysis_summary_data", conversationStateData.PostAnalysis?.SummaryData },
                    { "post_analysis_tagging_data", conversationStateData.PostAnalysis?.TagsData },
                    { "post_analysis_extraction_data", conversationStateData.PostAnalysis?.ExtractedFieldsData },
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetInboundTelephonyCampaignPostAnalysisArguements:EXCEPTION",
                    $"Error getting telephony campaign post analysis arguements: {ex.Message}"
                );
            }
        }

        // Web Session
        public async Task SendWebSessionCampaignAction(string webSessionId)
        {
            var webSessionData = await _webSessionRepository.GetWebSessionByIdAsync(webSessionId);
            if (webSessionData == null)
            {
                _logger.LogError("Unable to find web session {webSessionId} to send campaign action.", webSessionId);
                return;
            }

            if (webSessionData.Status == WebSessionStatusEnum.Queued ||
                webSessionData.Status == WebSessionStatusEnum.ProcessingQueue ||
                webSessionData.Status == WebSessionStatusEnum.ProcessingBackend
            )
            {
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebSessionCampaignAction");
            if (!businessDataResult.Success)
            {
                await _webSessionRepository.AddLogAsync(
                    webSessionData.Id,
                    new WebSessionLogEntry
                    {
                        Message = $"Unable to find business {webSessionData.BusinessId} for web session {webSessionId} to send campaign action: [{businessDataResult.Code}] {businessDataResult.Message}",
                        Type = WebSessionLogTypeEnum.Error
                    }
                );

                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebSessionCampaignAction");
            if (!businessAppResult.Success)
            {
                await _webSessionRepository.AddLogAsync(
                    webSessionData.Id,
                    new WebSessionLogEntry
                    {
                        Message = $"Unable to find business app for web session {webSessionId} to send campaign action: [{businessAppResult.Code}] {businessAppResult.Message} ",
                        Type = WebSessionLogTypeEnum.Error
                    }
                );

                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId);
            if (!webCampaignResult.Success)
            {
                await _webSessionRepository.AddLogAsync(
                    webSessionData.Id,
                    new WebSessionLogEntry
                    {
                        Message = $"Unable to find web campaign to send campaign action if any.",
                        Type = WebSessionLogTypeEnum.Error
                    }
                );

                return;
            }
            var webCampaignData = webCampaignResult.Data!;

            // Initiation Failure
            if (
                webSessionData.Status == WebSessionStatusEnum.Failed ||
                webSessionData.Status == WebSessionStatusEnum.Canceled ||
                webSessionData.Status == WebSessionStatusEnum.Expired
            ) {
                if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationInitiationFailureTool.ToolId)) return;

                var failureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationInitiationFailureTool.ToolId);
                if (failureToolData == null)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to find web campaign call initiation failure tool to find and send campaign action.",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var failureArgumentsResult = GetWebCampaignConversationInitiationFailureArguements(webSessionData);
                if (!failureArgumentsResult.Success)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to get web campaign call initiation failure tool arguements. [{failureArgumentsResult.Code}] {failureArgumentsResult.Message} ",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var failureArguments = failureArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = webCampaignData.Actions.ConversationInitiationFailureTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            failureArguments
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    failureToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to execute web campaign call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }
                else
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Web campaign call initiation failure tool response:\n```{executeActionToolResult.Data}```",
                            Type = WebSessionLogTypeEnum.Information
                        }
                    );
                }

                return;
            }
            else if (webSessionData.Status == WebSessionStatusEnum.ProcessedBackend)
            {
                if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationInitiatedTool.ToolId)) return;

                var conversationState = await _conversationStateRepository.GetByIdAsync(webSessionData.SessionId!);
                if (conversationState == null)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to find conversation state for web session to send campaign action.",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );
                }

                var successToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationInitiatedTool.ToolId);
                if (successToolData == null)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to find web campaign call initiation success tool to find and send campaign action.",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var successArgumentsResult = GetWebCampaignConversationInitiatedOrStartedArguements(webSessionData, conversationState!);
                if (!successArgumentsResult.Success)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to get web campaign call initiation success tool arguements. [{successArgumentsResult.Code}] {successArgumentsResult.Message} ",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var successArguments = successArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = webCampaignData.Actions.ConversationInitiatedTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            successArguments
                        );

                        finalToolArguments[argumentName] = processedValue;
                    }
                }

                var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                    successToolData,
                    finalToolArguments,
                    CancellationToken.None
                );
                if (!executeActionToolResult.Success)
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Unable to execute web campaign call initiation success tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = WebSessionLogTypeEnum.Error
                        }
                    );

                    return;
                }
                else
                {
                    await _webSessionRepository.AddLogAsync(
                        webSessionData.Id,
                        new WebSessionLogEntry
                        {
                            Message = $"Web campaign call initiation success tool response:\n```{executeActionToolResult.Data}```",
                            Type = WebSessionLogTypeEnum.Information
                        }
                    );
                }
            }
            else
            {
                await _webSessionRepository.AddLogAsync(
                    webSessionData.Id,
                    new WebSessionLogEntry
                    {
                        Message = $"Web session status is not failed or canceled. Status: {webSessionData.Status}.",
                        Type = WebSessionLogTypeEnum.Error
                    }
                );

                return;
            }
        }
        public async Task SendWebConversationSessionStartedCampaignAction(string webConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(webConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for web conversation session id {WebConversationSessionId} to run initiated action.", webConversationSessionId);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Active)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Conversation state is status {converationStateData.Status} for web conversation session id {webConversationSessionId} to run initiated action.",
                    }
                );

                return;
            }

            var webSessionData = await _webSessionRepository.GetWebSessionByIdAsync(webConversationSessionId);
            if (webSessionData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web session data for web conversation session id {webConversationSessionId} to run initiated action.",
                    }
                );

                return;
            }

            if (webSessionData.Status != WebSessionStatusEnum.ProcessedBackend)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Web session status is not processed backend for web conversation session id {webConversationSessionId} to run initiated action.",
                    }
                );

                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebConversationSessionInitiatedCampaignAction");
            if (!businessDataResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for web session id {webSessionData.Id} to send web campaign initiated action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebConversationSessionInitiatedCampaignAction");
            if (!businessAppResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send web campaign initiated action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId);
            if (!webCampaignResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web campaign data to send session campaign initiated action.",
                    }
                );
                return;
            }
            var webCampaignData = webCampaignResult.Data!;

            if (webCampaignData.Actions.ConversationStartedTool == null || string.IsNullOrEmpty(webCampaignData.Actions.ConversationStartedTool.ToolId)) return;

            var conversationStartedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationStartedTool.ToolId);
            if (conversationStartedToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find conversation started tool data with id {webCampaignData.Actions.ConversationStartedTool.ToolId} for web conversation session id {webConversationSessionId} to send conversation started action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var conversationStartedArgumentsResult = GetWebCampaignConversationInitiatedOrStartedArguements(webSessionData, converationStateData);
            if (!conversationStartedArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get call started arguments for web conversation session id {webConversationSessionId} to send conversation started action:[{conversationStartedArgumentsResult.Code}] {conversationStartedArgumentsResult.Message}.",
                    }
                );

                return;
            }
            var callStartedArguments = conversationStartedArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = webCampaignData.Actions.ConversationStartedTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        callStartedArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationStartedToolData,
                finalToolArguments,
                CancellationToken.None
            );

            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute conversation started tool.[{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );

                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Web campaign conversation started tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }
        }
        public async Task SendWebConversationSessionEndedCampaignAction(string webConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(webConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for web conversation session id {WebConversationSessionId} to run action.", webConversationSessionId);
                return;
            }

            if (converationStateData.Status != ConversationSessionState.Ended && converationStateData.Status != ConversationSessionState.Error)
            {
                _logger.LogError("Web conversation session id {WebConversationSessionId} invalid status (not ended/error/waiting for client) {Status} to run action.", webConversationSessionId, converationStateData.Status.ToString());

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Web conversation session id {webConversationSessionId} invalid status to run action if any.",
                    }
                );

                return;
            }

            var webSessionData = await _webSessionRepository.GetWebSessionByIdAsync(converationStateData.WebSessionId!);
            if (webSessionData == null)
            {
                _logger.LogError("Unable to find web session data for web conversation session id {WebConversationSessionId}.", webConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web session data for web conversation session id {webConversationSessionId} to run action if any.",
                    }
                );

                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebConversationSessionCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for web session id {WebSessionId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, webSessionData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for web session id {webSessionData.Id} to send web campaign action if any.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebConversationSessionCampaignAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send web campaign action if any.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId);
            if (!webCampaignResult.Success)
            {
                _logger.LogError("Unable to find web campaign data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, businessData.Id);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web campaign data to send session campaign action if any.",
                    }
                );
                return;
            }
            var webCampaignData = webCampaignResult.Data!;

            if (
                converationStateData.EndType == ConversationSessionEndType.UserEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.AgentEndedCall ||
                converationStateData.EndType == ConversationSessionEndType.UserSilenceTimeoutReached ||
                converationStateData.EndType == ConversationSessionEndType.MaxConversationDurationReached ||
                converationStateData.EndType == ConversationSessionEndType.MidSessionFailure
            )
            {
                if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationEndedTool.ToolId)) return;

                var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationEndedTool.ToolId!);
                if (conversationEndedToolData == null)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        webConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to find conversation ended tool data with id {webCampaignData.Actions.ConversationEndedTool.ToolId} for web conversation session id {webConversationSessionId} to send conversation end action.",
                        }
                    );
                    return;
                }

                CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
                toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

                var callEndedArgumentsResult = GetWebCampaignConversationEndArguements(webSessionData, converationStateData);
                if (!callEndedArgumentsResult.Success)
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        webConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to get call end arguments for web conversation session id {webConversationSessionId} to send conversation end action: [{callEndedArgumentsResult.Code}] {callEndedArgumentsResult.Message}.",
                        }
                    );

                    return;
                }
                var callEndedArguments = callEndedArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object?>();
                var configuredArguments = webCampaignData.Actions.ConversationEndedTool.Arguments;
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
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        webConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Error,
                            Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                        }
                    );

                    return;
                }
                else
                {
                    await _conversationStateLogsRepository.AddLogEntryAsync(
                        webConversationSessionId,
                        new ConversationStateLogEntry
                        {
                            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                            Level = ConversationStateLogLevelEnum.Information,
                            Message = $"Web campaign conversation ended tool response:\n```{executeActionToolResult.Data}```",
                        }
                    );
                }

                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Invalid end type {converationStateData.EndType} for web conversation session id {webConversationSessionId} to send conversation end action.",
                    }
                );
            }
        }
        public async Task SendWebConversationSessionPostAnalysisCampaignAction(string webConversationSessionId)
        {
            var converationStateData = await _conversationStateRepository.GetByIdAsync(webConversationSessionId);
            if (converationStateData == null)
            {
                _logger.LogError("Unable to find conversation state data for web conversation session id {WebConversationSessionId} to run post analysis action.", webConversationSessionId);
                return;
            }

            var webSessionData = await _webSessionRepository.GetWebSessionByIdAsync(converationStateData.WebSessionId!);
            if (webSessionData == null)
            {
                _logger.LogError("Unable to find web session data for web conversation session id {WebConversationSessionId}.", webConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web session data for web conversation session id {webConversationSessionId} to run post analysis action.",
                    }
                );
                return;
            }

            if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebConversationSessionPostAnalysisAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business data for web session id {WebSessionId} for web conversation session id {WebConversationSessionId}.", webSessionData.Id, webConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business data for web session id {webSessionData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessData = businessDataResult.Data!;

            var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebConversationSessionPostAnalysisAction");
            if (!businessAppResult.Success)
            {
                _logger.LogError("Unable to find business app data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", businessData.Id, webConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find business app data for business id {businessData.Id} to send session post analysis action.",
                    }
                );
                return;
            }
            var businessApp = businessAppResult.Data!;

            var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId!);
            if (!webCampaignResult.Success)
            {
                _logger.LogError("Unable to find web campaign data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", businessData.Id, webConversationSessionId);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find web campaign data to send session post analysis action.",
                    }
                );
                return;
            }
            var webCampaignData = webCampaignResult.Data!;

            if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationPostAnalysisTool.ToolId)) return;

            var conversationPostAnalysisToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationPostAnalysisTool.ToolId!);
            if (conversationPostAnalysisToolData == null)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to find conversation post analysis tool data with id {webCampaignData.Actions.ConversationPostAnalysisTool.ToolId} for web conversation session id {webConversationSessionId} to send post analysis action.",
                    }
                );
                return;
            }

            CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            var postAnalysisArgumentsResult = GetWebCampaignConversationPostAnalysisArguements(webSessionData, converationStateData);
            if (!postAnalysisArgumentsResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to get post analysis arguments for web conversation session id {webConversationSessionId} to send post analysis action: [{postAnalysisArgumentsResult.Code}] {postAnalysisArgumentsResult.Message}.",
                    }
                );
                return;
            }
            var postAnalysisArguments = postAnalysisArgumentsResult.Data!;

            var finalToolArguments = new Dictionary<string, object?>();
            var configuredArguments = webCampaignData.Actions.ConversationPostAnalysisTool.Arguments;
            if (configuredArguments != null)
            {
                foreach (var configuredArg in configuredArguments)
                {
                    var argumentName = configuredArg.Key;
                    var argumentTemplate = configuredArg.Value;

                    var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                        argumentTemplate.ToString()!,
                        postAnalysisArguments
                    );

                    finalToolArguments[argumentName] = processedValue;
                }
            }

            var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
                conversationPostAnalysisToolData,
                finalToolArguments,
                CancellationToken.None
            );
            if (!executeActionToolResult.Success)
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Unable to execute conversation post analysis tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                    }
                );
                return;
            }
            else
            {
                await _conversationStateLogsRepository.AddLogEntryAsync(
                    webConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Information,
                        Message = $"Web campaign post analysis tool response:\n```{executeActionToolResult.Data}```",
                    }
                );
            }
        }

        private FunctionReturnResult<Dictionary<string, object?>?> GetWebCampaignConversationInitiationFailureArguements(WebSessionData webSessionData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>?>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", (int)webSessionData.Status },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", (int)webSessionData.TransportType },
                    { "web_session_initiation_error", "Failed to initiate web session" }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebCampaignConversationInitiationFailureArguements:EXCEPTION",
                    $"Error getting web campaign conversation initiation failure arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetWebCampaignConversationInitiatedOrStartedArguements(WebSessionData webSessionData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", (int)webSessionData.Status },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", (int)webSessionData.TransportType },

                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebCampaignConversationInitiatedArguements:EXCEPTION",
                    $"Error getting web campaign conversation initiated arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetWebCampaignConversationEndArguements(WebSessionData webSessionData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", (int)webSessionData.Status },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", (int)webSessionData.TransportType },

                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) }
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebCampaignConversationEndArguements:EXCEPTION",
                    $"Error getting web campaign conversation end arguements: {ex.Message}"
                );
            }
        }
        private FunctionReturnResult<Dictionary<string, object?>> GetWebCampaignConversationPostAnalysisArguements(WebSessionData webSessionData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    // --- Web Session Data ---
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", (int)webSessionData.Status },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", (int)webSessionData.TransportType },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", (int)conversationStateData.EndType },
                    { "conversation_end_time", conversationStateData.EndTime },
                    { "conversation_turns", conversationStateData.Turns },
                    { "conversation_turns_simplified", ConversationTurnsCompiler.SimplifyConversationTurns(conversationStateData.Turns) },

                    // --- Post Analysis Data ---
                    { "post_analysis_template_id", conversationStateData.PostAnalysis?.PostAnalysisId },
                    { "post_analysis_status_type", (int?)conversationStateData.PostAnalysis?.Status },
                    { "post_analysis_summary_data", conversationStateData.PostAnalysis?.SummaryData },
                    { "post_analysis_tagging_data", conversationStateData.PostAnalysis?.TagsData },
                    { "post_analysis_extraction_data", conversationStateData.PostAnalysis?.ExtractedFieldsData },
                };

                return result.SetSuccessResult(resultData);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult(
                    "GetWebCampaignConversationPostAnalysisArguements:EXCEPTION",
                    $"Error getting web campaign post analysis arguements: {ex.Message}"
                );
            }
        }
    }
}

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
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundCallQueueTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business {businessId} for outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueData.BusinessId, outboundCallQueueId);

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
                _logger.LogError("Unable to find business app for outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueId);

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

            if (outboundCallQueueData.Status == CallQueueStatusEnum.Failed || outboundCallQueueData.Status == CallQueueStatusEnum.Canceled || outboundCallQueueData.Status == CallQueueStatusEnum.Expired)
            {
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
        }
        public async Task SendOutboundConversationSessionAnsweredTelephonyCampaignAction(string outboundConversationSessionId)
        {
            //var converationStateData = await _conversationStateRepository.GetByIdAsync(outboundConversationSessionId);
            //if (converationStateData == null)
            //{
            //    _logger.LogError("Unable to find conversation state data for outbound conversation session id {OutboundConversationSessionId} to run answered action.", outboundConversationSessionId);
            //    return;
            //}

            //var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueBySessionIdAsync(outboundConversationSessionId);
            //if (outboundCallQueueData == null)
            //{
            //    _logger.LogError("Unable to find outbound call queue data for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find outbound call queue data for outbound conversation session id {outboundConversationSessionId} to run answered action.",
            //        }
            //    );

            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(outboundCallQueueData.BusinessId, "SendOutboundConversationSessionTelephonyCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business data for outbound call queue id {OutboundCallQueueId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, outboundCallQueueData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business data for outbound call queue id {outboundCallQueueData.Id} to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendOutboundConversationSessionTelephonyCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(outboundCallQueueData.BusinessId, outboundCallQueueData.CampaignId);
            //if (!callQueueTelephonyCampaignResult.Success)
            //{
            //    _logger.LogError("Unable to find telephony campaign data for business id {BusinessId} for outbound conversation session id {OutboundConversationSessionId}.", outboundConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find telephony campaign data to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}
            //var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            //if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallPickedTool.ToolId)) return;

            //var conversationAnsweredToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(outboundCallQueueData.BusinessId, telephonyCampaign.Actions.CallPickedTool.ToolId!);
            //if (conversationAnsweredToolData == null)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find conversation answered tool data with id {telephonyCampaign.Actions.CallPickedTool.ToolId} for outbound conversation session id {outboundConversationSessionId} to send conversation answered action.",
            //        }
            //    );
            //    return;
            //}

            //CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //var callAnsweredArgumentsResult = GetTelephonyCampaignCallAnsweredArguements(outboundCallQueueData, converationStateData);
            //if (!callAnsweredArgumentsResult.Success)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to get call answered arguments for outbound conversation session id {outboundConversationSessionId} to send conversation answered action: [{callAnsweredArgumentsResult.Code}] {callAnsweredArgumentsResult.Message}.",
            //        }
            //    );

            //    return;
            //}
            //var callAnsweredArguments = callAnsweredArgumentsResult.Data!;

            //var finalToolArguments = new Dictionary<string, object?>();
            //var configuredArguments = telephonyCampaign.Actions.CallPickedTool.Arguments;
            //if (configuredArguments != null)
            //{
            //    foreach (var configuredArg in configuredArguments)
            //    {
            //        var argumentName = configuredArg.Key;
            //        var argumentTemplate = configuredArg.Value;

            //        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //            argumentTemplate.ToString()!,
            //            callAnsweredArguments
            //        );

            //        finalToolArguments[argumentName] = processedValue;
            //    }
            //}

            //var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //    conversationAnsweredToolData,
            //    finalToolArguments,
            //    CancellationToken.None
            //);
            //if (!executeActionToolResult.Success)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to execute conversation answered tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //        }
            //    );

            //    return;
            //}
            //else
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        outboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Information,
            //            Message = $"Telephony campaign call answered tool response:\n```{executeActionToolResult.Data}```",
            //        }
            //    );
            //}
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
                _logger.LogError("Outbound conversation session id {OutboundConversationSessionId} invalid status (not ended/error/waiting for client) {Status} to run action for reason {Reason}.", outboundConversationSessionId, converationStateData.Status.ToString(), reason);

                await _conversationStateLogsRepository.AddLogEntryAsync(
                    outboundConversationSessionId,
                    new ConversationStateLogEntry
                    {
                        SenderType = ConversationStateLogSenderTypeEnum.Conversation,
                        Level = ConversationStateLogLevelEnum.Error,
                        Message = $"Outbound conversation session id {outboundConversationSessionId} invalid status to run action if any for reason {reason}.",
                    }
                );

                return;
            }

            var outboundCallQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueBySessionIdAsync(outboundConversationSessionId);
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

        // Inbound Telephony
        public async Task SendInboundCallQueueTelephonyCampaignAction(string inboundCallQueueId, string logMessage)
        {
            //var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueByIdAsync(inboundCallQueueId);
            //if (inboundCallQueueData == null)
            //{
            //    _logger.LogError("Unable to find inbound call queue {inboundCallQueueId} to send campaign action.", inboundCallQueueId);
            //    return;
            //}

            //if (inboundCallQueueData.Status == CallQueueStatusEnum.Queued ||
            //    inboundCallQueueData.Status == CallQueueStatusEnum.ProcessingProxy ||
            //    inboundCallQueueData.Status == CallQueueStatusEnum.ProcessedProxy ||
            //    inboundCallQueueData.Status == CallQueueStatusEnum.ProcessingBackend
            //) {
            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundCallQueueTelephonyCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business {businessId} for inbound call queue {inboundCallQueueId} to send campaign action.", inboundCallQueueData.BusinessId, inboundCallQueueId);

            //    await _inboundCallQueueRepository.AddCallLogAsync(
            //        inboundCallQueueData.Id,
            //        new CallQueueLogEntry
            //        {
            //            Message = $"Unable to find business {inboundCallQueueData.BusinessId} for inbound call queue {inboundCallQueueId} to send campaign action: [{businessDataResult.Code}] {businessDataResult.Message}",
            //            Type = CallQueueLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundCallQueueTelephonyCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app for inbound call queue {inboundCallQueueId} to send campaign action.", inboundCallQueueId);

            //    await _inboundCallQueueRepository.AddCallLogAsync(
            //        inboundCallQueueData.Id,
            //        new CallQueueLogEntry
            //        {
            //            Message = $"Unable to find business app for inbound call queue {inboundCallQueueId} to send campaign action: [{businessAppResult.Code}] {businessAppResult.Message} ",
            //            Type = CallQueueLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            //var routeData = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            //if (routeData == null)
            //{
            //    await _inboundCallQueueRepository.AddCallLogAsync(
            //        inboundCallQueueData.Id,
            //        new CallQueueLogEntry
            //        {
            //            Message = $"Unable to find inbound route to send campaign action if any.",
            //            Type = CallQueueLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}

            //if (inboundCallQueueData.Status == CallQueueStatusEnum.Failed || inboundCallQueueData.Status == CallQueueStatusEnum.Canceled || inboundCallQueueData.Status == CallQueueStatusEnum.Expired)
            //{
            //    if (string.IsNullOrEmpty(routeData.Actions.CallInitiationFailureTool.SelectedToolId)) return;

            //    var callInitiationFailureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, routeData.Actions.CallInitiationFailureTool.SelectedToolId);
            //    if (callInitiationFailureToolData == null)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to find inbound route call initiation failure tool to find and send campaign action.",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var callFailureArgumentsResult = GetInboundTelephonyCampaignCallInitiationFailureArguements(inboundCallQueueData, logMessage);
            //    if (!callFailureArgumentsResult.Success)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to get inbound route call initiation failure tool arguements. [{callFailureArgumentsResult.Code}] {callFailureArgumentsResult.Message} ",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    var callFailureArguments = callFailureArgumentsResult.Data!;

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = routeData.Actions.CallInitiationFailureTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                callFailureArguments
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        callInitiationFailureToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to execute inbound route call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Inbound route call initiation failure tool response:\n```{executeActionToolResult.Data}```",
            //                Type = CallQueueLogTypeEnum.Information
            //            }
            //        );
            //    }

            //    return;
            //}
            //else if (inboundCallQueueData.Status == CallQueueStatusEnum.ProcessedBackend)
            //{
            //    if (string.IsNullOrEmpty(routeData.Actions.RingingTool.SelectedToolId)) return;

            //    var conversationState = await _conversationStateRepository.GetByIdAsync(inboundCallQueueData.SessionId!);
            //    if (conversationState == null)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to find inbound route conversation session to send ringing campaign action.",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );
            //    }

            //    var ringingToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, routeData.Actions.RingingTool.SelectedToolId);
            //    if (ringingToolData == null)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to find inbound route ringing tool to find and send ringing campaign action.",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var ringingArgumentsResult = GetInboundTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(inboundCallQueueData);
            //    if (!ringingArgumentsResult.Success)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to get inbound route ringing tool arguements. [{ringingArgumentsResult.Code}] {ringingArgumentsResult.Message} ",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = routeData.Actions.RingingTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                ringingArgumentsResult.Data!
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        ringingToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Unable to execute inbound route ringing tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //                Type = CallQueueLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _inboundCallQueueRepository.AddCallLogAsync(
            //            inboundCallQueueData.Id,
            //            new CallQueueLogEntry
            //            {
            //                Message = $"Inbound route ringing tool response:\n```{executeActionToolResult.Message}```",
            //                Type = CallQueueLogTypeEnum.Information
            //            }
            //        );
            //    }

            //    return;
            //}
        }
        public async Task SendInboundConversationSessionAnsweredTelephonyCampaignAction(string inboundConversationSessionId)
        {
            //var converationStateData = await _conversationStateRepository.GetByIdAsync(inboundConversationSessionId);
            //if (converationStateData == null)
            //{
            //    _logger.LogError("Unable to find conversation state data for inbound conversation session id {InboundConversationSessionId} to run answered action.", inboundConversationSessionId);
            //    return;
            //}

            //var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueBySessionIdAsync(inboundConversationSessionId);
            //if (inboundCallQueueData == null)
            //{
            //    _logger.LogError("Unable to find inbound call queue data for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find inbound call queue data for inbound conversation session id {inboundConversationSessionId} to run answered action.",
            //        }
            //    );

            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundConversationSessionTelephonyCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business data for inbound call queue id {InboundCallQueueId} for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId, inboundCallQueueData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business data for inbound call queue id {inboundCallQueueData.Id} to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundConversationSessionTelephonyCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            //var routeData = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            //if (routeData == null)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find inbound route data to send session telephony campaign answered action.",
            //        }
            //    );
            //    return;
            //}

            //if (string.IsNullOrEmpty(routeData.Actions.CallPickedTool.SelectedToolId)) return;

            //var conversationAnsweredToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, routeData.Actions.CallPickedTool.SelectedToolId);
            //if (conversationAnsweredToolData == null)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find conversation answered tool data with id {routeData.Actions.CallPickedTool.SelectedToolId} for inbound conversation session id {inboundConversationSessionId} to send conversation answered action.",
            //        }
            //    );
            //    return;
            //}

            //CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //var callAnsweredArgumentsResult = GetInboundTelephonyCampaignCallAnsweredArguements(inboundCallQueueData, converationStateData);
            //if (!callAnsweredArgumentsResult.Success)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to get call answered arguments for inbound conversation session id {inboundConversationSessionId} to send conversation answered action: [{callAnsweredArgumentsResult.Code}] {callAnsweredArgumentsResult.Message}.",
            //        }
            //    );

            //    return;
            //}
            //var callAnsweredArguments = callAnsweredArgumentsResult.Data!;

            //var finalToolArguments = new Dictionary<string, object?>();
            //var configuredArguments = routeData.Actions.CallPickedTool.Arguments;
            //if (configuredArguments != null)
            //{
            //    foreach (var configuredArg in configuredArguments)
            //    {
            //        var argumentName = configuredArg.Key;
            //        var argumentTemplate = configuredArg.Value;

            //        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //            argumentTemplate.ToString()!,
            //            callAnsweredArguments
            //        );

            //        finalToolArguments[argumentName] = processedValue;
            //    }
            //}

            //var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //    conversationAnsweredToolData,
            //    finalToolArguments,
            //    CancellationToken.None
            //);
            //if (!executeActionToolResult.Success)
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to execute conversation answered tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //        }
            //    );

            //    return;
            //}
            //else
            //{
            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Information,
            //            Message = $"Inbound route call answered tool response:\n```{executeActionToolResult.Data}```",
            //        }
            //    );
            //}
        }
        public async Task SendInboundConversationSessionEndedTelephonyCampaignAction(string inboundConversationSessionId)
        {
            //var converationStateData = await _conversationStateRepository.GetByIdAsync(inboundConversationSessionId);
            //if (converationStateData == null)
            //{
            //    _logger.LogError("Unable to find conversation state data for inbound conversation session id {InboundConversationSessionId} to run action.", inboundConversationSessionId);
            //    return;
            //}

            //if (converationStateData.Status != ConversationSessionState.Ended && converationStateData.Status != ConversationSessionState.Error)
            //{
            //    _logger.LogError("Inbound conversation session id {InboundConversationSessionId} invalid status (not ended/error/waiting for client) {Status} to run action.", inboundConversationSessionId, converationStateData.Status.ToString());

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Inbound conversation session id {inboundConversationSessionId} invalid status to run action if any.",
            //        }
            //    );

            //    return;
            //}

            //var inboundCallQueueData = await _inboundCallQueueRepository.GetInboundCallQueueBySessionIdAsync(inboundConversationSessionId);
            //if (inboundCallQueueData == null)
            //{
            //    _logger.LogError("Unable to find inbound call queue data for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find inbound call queue data for inbound conversation session id {inboundConversationSessionId} to run action if any.",
            //        }
            //    );

            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(inboundCallQueueData.BusinessId, "SendInboundConversationSessionTelephonyCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business data for inbound call queue id {InboundCallQueueId} for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId, inboundCallQueueData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business data for inbound call queue id {inboundCallQueueData.Id} to send session telephony campaign action if any.",
            //        }
            //    );
            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendInboundConversationSessionTelephonyCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business app data for business id {businessData.Id} to send session telephony campaign action if any.",
            //        }
            //    );
            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //if (string.IsNullOrEmpty(inboundCallQueueData.RouteId)) return;

            //var routeData = await _businessManager.GetRoutesManager().GetBusinessRoute(inboundCallQueueData.BusinessId, inboundCallQueueData.RouteId);
            //if (routeData == null)
            //{
            //    _logger.LogError("Unable to find inbound route data for business id {BusinessId} for inbound conversation session id {InboundConversationSessionId}.", inboundConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        inboundConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find inbound route data to send session telephony campaign action if any.",
            //        }
            //    );
            //    return;
            //}

            //if (
            //    converationStateData.EndType == ConversationSessionEndType.UserEndedCall ||
            //    converationStateData.EndType == ConversationSessionEndType.AgentEndedCall ||
            //    converationStateData.EndType == ConversationSessionEndType.UserSilenceTimeoutReached ||
            //    converationStateData.EndType == ConversationSessionEndType.MaxConversationDurationReached ||
            //    converationStateData.EndType == ConversationSessionEndType.MidSessionFailure
            //) {
            //    if (string.IsNullOrEmpty(routeData.Actions.CallEndedTool.SelectedToolId)) return;

            //    var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, routeData.Actions.CallEndedTool.SelectedToolId!);
            //    if (conversationEndedToolData == null)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to find conversation ended tool data with id {routeData.Actions.CallEndedTool.SelectedToolId} for inbound conversation session id {inboundConversationSessionId} to send conversation end action.",
            //            }
            //        );
            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var callEndedArgumentsResult = GetInboundTelephonyCampaignCallEndArguements(inboundCallQueueData, converationStateData);
            //    if (!callEndedArgumentsResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to get call end arguments for inbound conversation session id {inboundConversationSessionId} to send conversation end action: [{callEndedArgumentsResult.Code}] {callEndedArgumentsResult.Message}.",
            //            }
            //        );

            //        return;
            //    }
            //    var callEndedArguments = callEndedArgumentsResult.Data!;

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = routeData.Actions.CallEndedTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                callEndedArguments
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        conversationEndedToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Information,
            //                Message = $"Inbound route call ended tool response:\n```{executeActionToolResult.Data}```",
            //            }
            //        );
            //    }

            //    return;
            //}
            //else if (converationStateData.EndType == ConversationSessionEndType.UserNoAnswer || converationStateData.EndType == ConversationSessionEndType.UserDeclinedOrBusy)
            //{
            //    if (string.IsNullOrEmpty(routeData.Actions.CallMissedTool.SelectedToolId)) return;

            //    var conversationMissedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(inboundCallQueueData.BusinessId, routeData.Actions.CallMissedTool.SelectedToolId!);
            //    if (conversationMissedToolData == null)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to find conversation missed tool data with id {routeData.Actions.CallMissedTool.SelectedToolId} for inbound conversation session id {inboundConversationSessionId} to send conversation end (missed) action.",
            //            }
            //        );
            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var callMissedArgumentsResult = GetInboundTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(inboundCallQueueData);
            //    if (!callMissedArgumentsResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to get call missed arguments for inbound conversation session id {inboundConversationSessionId} to send conversation end (missed) action: [{callMissedArgumentsResult.Code}] {callMissedArgumentsResult.Message}",
            //            }
            //        );

            //        return;
            //    }
            //    var callMissedArguments = callMissedArgumentsResult.Data!;

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = routeData.Actions.CallMissedTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                callMissedArguments
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        conversationMissedToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to execute conversation missed tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            inboundConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Information,
            //                Message = $"Inbound route call missed tool response:\n```{executeActionToolResult.Data}```",
            //            }
            //        );
            //    }

            //    return;
            //}
        }
        private FunctionReturnResult<Dictionary<string, object?>?> GetInboundTelephonyCampaignCallInitiatedOrDeclinedOrMissedArguements(InboundCallQueueData callQueueData)
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

                    // InboundCallQueueData specific fields
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", callQueueData.RouteNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },
            
                    // Conversation related
                    { "conversation_id", callQueueData.SessionId }
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
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_session_id", callQueueData.SessionId },

                    // InboundCallQueueData specific fields
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", callQueueData.RouteNumberProvider.ToString() },
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
        private FunctionReturnResult<Dictionary<string, object?>> GetInboundTelephonyCampaignCallAnsweredArguements(InboundCallQueueData callQueueData, ConversationState conversationStateData)
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
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", callQueueData.RouteNumberProvider.ToString() },
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
                    { "call_queue_status", callQueueData.Status.ToString() },
                    { "call_queue_route_id", callQueueData.RouteId },
                    { "call_queue_route_number_id", callQueueData.RouteNumberId },
                    { "call_queue_route_number_provider", callQueueData.RouteNumberProvider.ToString() },
                    { "call_queue_provider_call_id", callQueueData.ProviderCallId },
                    { "call_queue_caller_number", callQueueData.CallerNumber },

                    // --- Conversation Data ---
                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
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

        // Web Session
        public async Task SendWebSessionCampaignAction(string webSessionId)
        {
            //var webSessionData = await _webSessionRepository.GetWebSessionByIdAsync(webSessionId);
            //if (webSessionData == null)
            //{
            //    _logger.LogError("Unable to find web session {webSessionId} to send campaign action.", webSessionId);
            //    return;
            //}

            //if (webSessionData.Status == WebSessionStatusEnum.Queued ||
            //    webSessionData.Status == WebSessionStatusEnum.ProcessingProxy ||
            //    webSessionData.Status == WebSessionStatusEnum.ProcessedProxy ||
            //    webSessionData.Status == WebSessionStatusEnum.ProcessingBackend
            //) {
            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebSessionCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business {businessId} for web session {webSessionId} to send campaign action.", webSessionData.BusinessId, webSessionId);

            //    await _webSessionRepository.AddLogAsync(
            //        webSessionData.Id,
            //        new WebSessionLog
            //        {
            //            Message = $"Unable to find business {webSessionData.BusinessId} for web session {webSessionId} to send campaign action: [{businessDataResult.Code}] {businessDataResult.Message}",
            //            Type = WebSessionLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebSessionCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app for web session {webSessionId} to send campaign action.", webSessionId);

            //    await _webSessionRepository.AddLogAsync(
            //        webSessionData.Id,
            //        new WebSessionLog
            //        {
            //            Message = $"Unable to find business app for web session {webSessionId} to send campaign action: [{businessAppResult.Code}] {businessAppResult.Message} ",
            //            Type = WebSessionLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            //var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId);
            //if (!webCampaignResult.Success)
            //{
            //    await _webSessionRepository.AddLogAsync(
            //        webSessionData.Id,
            //        new WebSessionLog
            //        {
            //            Message = $"Unable to find web campaign to send campaign action if any.",
            //            Type = WebSessionLogTypeEnum.Error
            //        }
            //    );

            //    return;
            //}
            //var webCampaignData = webCampaignResult.Data!;

            //if (webSessionData.Status == WebSessionStatusEnum.Failed || webSessionData.Status == WebSessionStatusEnum.Canceled)
            //{
            //    if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationInitiationFailureTool.ToolId)) return;

            //    var failureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationInitiationFailureTool.ToolId);
            //    if (failureToolData == null)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to find web campaign call initiation failure tool to find and send campaign action.",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var failureArgumentsResult = GetWebCampaignConversationInitiationFailureArguements(webSessionData);
            //    if (!failureArgumentsResult.Success)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to get web campaign call initiation failure tool arguements. [{failureArgumentsResult.Code}] {failureArgumentsResult.Message} ",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    var failureArguments = failureArgumentsResult.Data!;

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = webCampaignData.Actions.ConversationInitiationFailureTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                failureArguments
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        failureToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to execute web campaign call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Web campaign call initiation failure tool response:\n```{executeActionToolResult.Data}```",
            //                Type = WebSessionLogTypeEnum.Information
            //            }
            //        );
            //    }

            //    return;
            //}
            //else if (webSessionData.Status == WebSessionStatusEnum.ProcessedBackend)
            //{
            //    if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationInitiatedTool.ToolId)) return;

            //    var conversationState = await _conversationStateRepository.GetByIdAsync(webSessionData.SessionId!);
            //    if (conversationState == null)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to find web session conversation to send initiated campaign action.",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );
            //    }

            //    var initiatedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationInitiatedTool.ToolId);
            //    if (initiatedToolData == null)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to find web campaign initiated tool to find and send initiated campaign action.",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var initiatedArgumentsResult = GetWebCampaignConversationInitiatedArguements(webSessionData, conversationState);
            //    if (!initiatedArgumentsResult.Success)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to get web campaign initiated tool arguements. [{initiatedArgumentsResult.Code}] {initiatedArgumentsResult.Message} ",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = webCampaignData.Actions.ConversationInitiatedTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                initiatedArgumentsResult.Data!
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        initiatedToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Unable to execute web campaign initiated tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //                Type = WebSessionLogTypeEnum.Error
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _webSessionRepository.AddLogAsync(
            //            webSessionData.Id,
            //            new WebSessionLog
            //            {
            //                Message = $"Web campaign initiated tool response:\n```{executeActionToolResult.Message}```",
            //                Type = WebSessionLogTypeEnum.Information
            //            }
            //        );
            //    }

            //    return;
            //}
        }
        public async Task SendWebConversationSessionCampaignAction(string webConversationSessionId)
        {
            //var converationStateData = await _conversationStateRepository.GetByIdAsync(webConversationSessionId);
            //if (converationStateData == null)
            //{
            //    _logger.LogError("Unable to find conversation state data for web conversation session id {WebConversationSessionId} to run action.", webConversationSessionId);
            //    return;
            //}

            //if (converationStateData.Status != ConversationSessionState.Ended && converationStateData.Status != ConversationSessionState.Error)
            //{
            //    _logger.LogError("Web conversation session id {WebConversationSessionId} invalid status (not ended/error/waiting for client) {Status} to run action.", webConversationSessionId, converationStateData.Status.ToString());

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        webConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Web conversation session id {webConversationSessionId} invalid status to run action if any.",
            //        }
            //    );

            //    return;
            //}

            //var webSessionData = await _webSessionRepository.GetWebSessionBySessionIdAsync(webConversationSessionId);
            //if (webSessionData == null)
            //{
            //    _logger.LogError("Unable to find web session data for web conversation session id {WebConversationSessionId}.", webConversationSessionId);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        webConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find web session data for web conversation session id {webConversationSessionId} to run action if any.",
            //        }
            //    );

            //    return;
            //}

            //var businessDataResult = await _businessManager.GetUserBusinessById(webSessionData.BusinessId, "SendWebConversationSessionCampaignAction");
            //if (!businessDataResult.Success)
            //{
            //    _logger.LogError("Unable to find business data for web session id {WebSessionId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, webSessionData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        webConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business data for web session id {webSessionData.Id} to send web campaign action if any.",
            //        }
            //    );
            //    return;
            //}
            //var businessData = businessDataResult.Data!;

            //var businessAppResult = await _businessManager.GetUserBusinessAppById(businessData.Id, "SendWebConversationSessionCampaignAction");
            //if (!businessAppResult.Success)
            //{
            //    _logger.LogError("Unable to find business app data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        webConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find business app data for business id {businessData.Id} to send web campaign action if any.",
            //        }
            //    );
            //    return;
            //}
            //var businessApp = businessAppResult.Data!;

            //if (string.IsNullOrEmpty(webSessionData.WebCampaignId)) return;

            //var webCampaignResult = await _businessManager.GetCampaignManager().GetWebCampaignById(webSessionData.BusinessId, webSessionData.WebCampaignId);
            //if (!webCampaignResult.Success)
            //{
            //    _logger.LogError("Unable to find web campaign data for business id {BusinessId} for web conversation session id {WebConversationSessionId}.", webConversationSessionId, businessData.Id);

            //    await _conversationStateLogsRepository.AddLogEntryAsync(
            //        webConversationSessionId,
            //        new ConversationStateLogEntry
            //        {
            //            SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //            Level = ConversationStateLogLevelEnum.Error,
            //            Message = $"Unable to find web campaign data to send session campaign action if any.",
            //        }
            //    );
            //    return;
            //}
            //var webCampaignData = webCampaignResult.Data!;

            //if (
            //    converationStateData.EndType == ConversationSessionEndType.UserEndedCall ||
            //    converationStateData.EndType == ConversationSessionEndType.AgentEndedCall ||
            //    converationStateData.EndType == ConversationSessionEndType.UserSilenceTimeoutReached ||
            //    converationStateData.EndType == ConversationSessionEndType.MaxConversationDurationReached ||
            //    converationStateData.EndType == ConversationSessionEndType.MidSessionFailure
            //) {
            //    if (string.IsNullOrEmpty(webCampaignData.Actions.ConversationEndedTool.ToolId)) return;

            //    var conversationEndedToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(webSessionData.BusinessId, webCampaignData.Actions.ConversationEndedTool.ToolId!);
            //    if (conversationEndedToolData == null)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            webConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to find conversation ended tool data with id {webCampaignData.Actions.ConversationEndedTool.ToolId} for web conversation session id {webConversationSessionId} to send conversation end action.",
            //            }
            //        );
            //        return;
            //    }

            //    CustomToolExecutionHelper toolExecutionHelper = new CustomToolExecutionHelper(_loggerFactory);
            //    toolExecutionHelper.Initialize(businessApp, businessData.DefaultLanguage);

            //    var callEndedArgumentsResult = GetWebCampaignConversationEndArguements(webSessionData, converationStateData);
            //    if (!callEndedArgumentsResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            webConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to get call end arguments for web conversation session id {webConversationSessionId} to send conversation end action: [{callEndedArgumentsResult.Code}] {callEndedArgumentsResult.Message}.",
            //            }
            //        );

            //        return;
            //    }
            //    var callEndedArguments = callEndedArgumentsResult.Data!;

            //    var finalToolArguments = new Dictionary<string, object?>();
            //    var configuredArguments = webCampaignData.Actions.ConversationEndedTool.Arguments;
            //    if (configuredArguments != null)
            //    {
            //        foreach (var configuredArg in configuredArguments)
            //        {
            //            var argumentName = configuredArg.Key;
            //            var argumentTemplate = configuredArg.Value;

            //            var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
            //                argumentTemplate.ToString()!,
            //                callEndedArguments
            //            );

            //            finalToolArguments[argumentName] = processedValue;
            //        }
            //    }

            //    var executeActionToolResult = await toolExecutionHelper.ExecuteHttpRequestForToolWithObjectDictAsync(
            //        conversationEndedToolData,
            //        finalToolArguments,
            //        CancellationToken.None
            //    );
            //    if (!executeActionToolResult.Success)
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            webConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Error,
            //                Message = $"Unable to execute conversation ended tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
            //            }
            //        );

            //        return;
            //    }
            //    else
            //    {
            //        await _conversationStateLogsRepository.AddLogEntryAsync(
            //            webConversationSessionId,
            //            new ConversationStateLogEntry
            //            {
            //                SenderType = ConversationStateLogSenderTypeEnum.Conversation,
            //                Level = ConversationStateLogLevelEnum.Information,
            //                Message = $"Web campaign conversation ended tool response:\n```{executeActionToolResult.Data}```",
            //            }
            //        );
            //    }

            //    return;
            //}
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
                    { "web_session_status", webSessionData.Status.ToString() },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", webSessionData.TransportType.ToString() },
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
        private FunctionReturnResult<Dictionary<string, object?>> GetWebCampaignConversationInitiatedArguements(WebSessionData webSessionData, ConversationState conversationStateData)
        {
            var result = new FunctionReturnResult<Dictionary<string, object?>>();

            try
            {
                var resultData = new Dictionary<string, object?>
                {
                    { "web_session_id", webSessionData.Id },
                    { "web_session_created_at", webSessionData.CreatedAt },
                    { "web_session_status", webSessionData.Status.ToString() },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", webSessionData.TransportType.ToString() },

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
                    { "web_session_status", webSessionData.Status.ToString() },
                    { "web_session_campaign_id", webSessionData.WebCampaignId },
                    { "web_session_region_id", webSessionData.RegionId },
                    { "web_session_client_identifier", webSessionData.ClientIdentifier },
                    { "web_session_dynamic_variables", webSessionData.DynamicVariables },
                    { "web_session_metadata", webSessionData.Metadata },
                    { "web_session_transport_type", webSessionData.TransportType.ToString() },

                    { "conversation_id", conversationStateData.Id },
                    { "conversation_start_time", conversationStateData.StartTime },
                    { "conversation_end_type", conversationStateData.EndType.ToString() },
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
    }
}

using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helpers;
using IqraInfrastructure.Helpers;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Conversation.Session.Agent.AI.Helpers;
using IqraInfrastructure.Repositories.Call;
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
        private readonly BusinessManager _businessManager;

        public CampaignActionExecutorService(
            ILoggerFactory loggerFactory,
            InboundCallQueueRepository inboundCallQueueRepository,
            OutboundCallQueueRepository outboundCallQueueRepository,
            WebSessionRepository webSessionRepository,
            BusinessManager businessManager
        ) {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CampaignActionExecutorService>();
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepo = outboundCallQueueRepository;
            _webSessionRepository = webSessionRepository;
            _businessManager = businessManager;
        }

        public async Task SendOutboundCallQueueTelephonyCampaignAction(string outboundCallQueueId, string logMessage)
        {
            var callQueueData = await _outboundCallQueueRepo.GetOutboundCallQueueByIdAsync(outboundCallQueueId);
            if (callQueueData == null)
            {
                _logger.LogError("Unable to find outbound call queue {outboundCallQueueId} to send campaign action.", outboundCallQueueId);
                return;
            }

            if (callQueueData.Status == CallQueueStatusEnum.Queued ||
                callQueueData.Status == CallQueueStatusEnum.ProcessingProxy ||
                callQueueData.Status == CallQueueStatusEnum.ProcessedProxy ||
                callQueueData.Status == CallQueueStatusEnum.ProcessingBackend ||
                callQueueData.Status == CallQueueStatusEnum.ProcessingBackend
            ) {
                return;
            }

            var businessDataResult = await _businessManager.GetUserBusinessById(callQueueData.BusinessId, "SendOutboundCallQueueTelephonyCampaignAction");
            if (!businessDataResult.Success)
            {
                _logger.LogError("Unable to find business {businessId} for outbound call queue {outboundCallQueueId} to send campaign action.", callQueueData.BusinessId, outboundCallQueueId);

                await _outboundCallQueueRepo.AddCallLogAsync(
                    callQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find business {callQueueData.BusinessId} for outbound call queue {outboundCallQueueId} to send campaign action: [{businessDataResult.Code}] {businessDataResult.Message}",
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
                    callQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find business app for outbound call queue {outboundCallQueueId} to send campaign action: [{businessAppResult.Code}] {businessAppResult.Message} ",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var businessApp = businessAppResult.Data!;

            var callQueueTelephonyCampaignResult = await _businessManager.GetCampaignManager().GetTelephonyCampaignById(callQueueData.BusinessId, callQueueData.CampaignId);
            if (!callQueueTelephonyCampaignResult.Success)
            {
                await _outboundCallQueueRepo.AddCallLogAsync(
                    callQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Unable to find call queue campaign to find and send campaign action if any. [{callQueueTelephonyCampaignResult.Code}] {callQueueTelephonyCampaignResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    }
                );

                return;
            }
            var telephonyCampaign = callQueueTelephonyCampaignResult.Data!;

            if (callQueueData.Status == CallQueueStatusEnum.Failed || callQueueData.Status == CallQueueStatusEnum.Canceled || callQueueData.Status == CallQueueStatusEnum.Expired)
            {
                if (string.IsNullOrEmpty(telephonyCampaign.Actions.CallInitiationFailureTool.ToolId)) return;

                var callInitiationFailureToolData = await _businessManager.GetToolsManager().GetBusinessAppTool(callQueueData.BusinessId, telephonyCampaign.Actions.CallInitiationFailureTool.ToolId);
                if (callInitiationFailureToolData == null)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        callQueueData.Id,
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

                var callFailureArgumentsResult = GetTelephonyCampaignCallInitiationFailureArguements(callQueueData, logMessage);
                if (!callFailureArgumentsResult.Success)
                {
                    await _outboundCallQueueRepo.AddCallLogAsync(
                        callQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to get call queue campaign call initiation failure tool arguements. [{callFailureArgumentsResult.Code}] {callFailureArgumentsResult.Message} ",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }
                var callFailureArguments = callFailureArgumentsResult.Data!;

                var finalToolArguments = new Dictionary<string, object>();
                var configuredArguments = telephonyCampaign.Actions.CallInitiationFailureTool.Arguments;
                if (configuredArguments != null)
                {
                    foreach (var configuredArg in configuredArguments)
                    {
                        var argumentName = configuredArg.Key;
                        var argumentTemplate = configuredArg.Value;

                        var processedValue = CustomVariableInputTemplateService.ProcessTemplateToObject(
                            argumentTemplate.ToString()!,
                            callFailureArgumentsResult.Data!
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
                        callQueueData.Id,
                        new CallQueueLog
                        {
                            Message = $"Unable to execute call queue campaign call initiation failure tool. [{executeActionToolResult.Code}] {executeActionToolResult.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        }
                    );

                    return;
                }

                await _outboundCallQueueRepo.AddCallLogAsync(
                    callQueueData.Id,
                    new CallQueueLog
                    {
                        Message = $"Call queue campaign call initiation failure tool response:\n```{executeActionToolResult.Message}```",
                        Type = CallQueueLogTypeEnum.Information
                    }
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
    }
}

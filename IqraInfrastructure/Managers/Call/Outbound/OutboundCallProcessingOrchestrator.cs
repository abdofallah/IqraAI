using IqraCore.Entities.Business;
using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Business;
using IqraInfrastructure.Managers.Integrations;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Managers.Telephony;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Call.Outbound
{
    public class OutboundCallProcessingOrchestrator
    {
        private readonly ILogger<OutboundCallProcessingOrchestrator> _logger;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly UserUsageValidationManager _billingValidationManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly BusinessManager _businessManager;
        private readonly RegionManager _regionManager;
        private readonly ModemTelManager _modemTelManager;
        private readonly TwilioManager _twilioManager;
        private readonly IntegrationsManager _integrationsManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CampaignActionExecutorService _campaignActionExecutorService;

        private readonly JsonSerializerOptions _camelCaseSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OutboundCallProcessingOrchestrator(
            ILogger<OutboundCallProcessingOrchestrator> logger,
            OutboundCallQueueRepository outboundCallQueueRepo,
            UserUsageValidationManager billingValidationManager,
            ServerSelectionManager serverSelectionManager,
            BusinessManager businessManager,
            RegionManager regionManager,
            ModemTelManager modemTelManager,
            TwilioManager twilioManager,
            IntegrationsManager integrationsManager,
            IHttpClientFactory httpClientFactory,
            CampaignActionExecutorService campaignActionExecutorService
        )
        {
            _logger = logger;
            _outboundCallQueueRepo = outboundCallQueueRepo;
            _billingValidationManager = billingValidationManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
            _businessManager = businessManager;
            _modemTelManager = modemTelManager;
            _twilioManager = twilioManager;
            _integrationsManager = integrationsManager;
            _httpClientFactory = httpClientFactory;
            _campaignActionExecutorService = campaignActionExecutorService;
        }

        public async Task ProcessCallAsync(OutboundCallQueueData callQueueData)
        {
            if (callQueueData.MaxScheduleForDateTime <= DateTime.UtcNow)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Expired,
                    new CallQueueLog {
                        Type = CallQueueLogTypeEnum.Information,
                        Message = "Call expired by reaching max schedule date time."
                    },
                    completedAt: DateTime.UtcNow
                );
            }

            var validationResult = await _billingValidationManager.ValidateCallPermissionAsync(callQueueData.BusinessId, true);
            if (!validationResult.Success)
            {
                if (
                    !string.IsNullOrWhiteSpace(validationResult.Code)
                    &&
                    (validationResult.Code.Contains("USER_CONCURRENCY_LIMIT") || validationResult.Code.Contains("BUSINESS_CONCURRENCY_LIMIT"))
                )
                {
                    await OnUpdateCallQueueStatusAndSendCampaignAction(
                        callQueueData,
                        CallQueueStatusEnum.Queued,
                        new CallQueueLog {
                            Message = $"Validation failed (will retry): {validationResult.Message}",
                            Type = CallQueueLogTypeEnum.Information
                        }
                    );
                    return;
                }

                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLog {
                        Message = $"Validation failed: [{validationResult.Code}] {validationResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            var businessPhoneNumber = await _businessManager.GetNumberManager().GetBusinessNumberById(callQueueData.BusinessId, callQueueData.CallingNumberId);
            if (businessPhoneNumber == null)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLog {
                        Message = $"System error: Business number {callQueueData.CallingNumberId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
            }

            var businessPhoneIntegration = await _businessManager.GetIntegrationsManager().getBusinessIntegrationById(callQueueData.BusinessId, businessPhoneNumber.IntegrationId);
            if (businessPhoneIntegration == null)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Canceled,
                    new CallQueueLog {
                        Message = $"System error: Business number {callQueueData.CallingNumberId} integration {businessPhoneNumber.IntegrationId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
            }

            switch (businessPhoneNumber.Provider)
            {
                case TelephonyProviderEnum.ModemTel:
                    {
                        var modemTelPhonenumberId = ((BusinessNumberModemTelData)businessPhoneNumber).ModemTelPhoneNumberId;
                        var modemtelStatusToCheck = new List<string>() { "Queued", "CallInProgress", "RingingIncoming", "RingingOutgoing" };

                        var currentNumberCalls = await _modemTelManager.GetCallsByStatusForPhoneNumber(_integrationsManager.DecryptField(businessPhoneIntegration.Data.EncryptedFields["apikey"]), businessPhoneIntegration.Data.Fields["endpoint"], modemTelPhonenumberId, modemtelStatusToCheck, 1);
                        if (!currentNumberCalls.Success)
                        {
                            await OnUpdateCallQueueStatusAndSendCampaignAction(
                                callQueueData,
                                CallQueueStatusEnum.Canceled,
                                new CallQueueLog {
                                    Message = $"[{currentNumberCalls.Code}] {currentNumberCalls.Message}",
                                    Type = CallQueueLogTypeEnum.Error
                                },
                                completedAt: DateTime.UtcNow
                            );
                            return;
                        }

                        if (currentNumberCalls.Data.Count > 0)
                        {
                            await OnUpdateCallQueueStatusAndSendCampaignAction(callQueueData, CallQueueStatusEnum.Queued);
                            return;
                        }
                        break;
                    }

                case TelephonyProviderEnum.Twilio:
                    {
                        await OnUpdateCallQueueStatusAndSendCampaignAction(callQueueData, CallQueueStatusEnum.Queued);
                        break;
                    }

                default:
                    {
                        await OnUpdateCallQueueStatusAndSendCampaignAction(
                            callQueueData,
                            CallQueueStatusEnum.Canceled,
                            new CallQueueLog {
                                Message = $"Unknown calling number provider: {businessPhoneNumber.Provider}.",
                                Type = CallQueueLogTypeEnum.Error
                            },
                            completedAt: DateTime.UtcNow
                        );
                        return;
                    }
            }

            await OnUpdateCallQueueStatusAndSendCampaignAction(
                callQueueData,
                CallQueueStatusEnum.ProcessingProxy,
                new CallQueueLog {
                    Message = $"Processing Queue within proxy",
                    Type = CallQueueLogTypeEnum.Information
                }
            );

            var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(callQueueData.RegionId);
            if (!serverSelectionResult.Success || !serverSelectionResult.Data.Any())
            {
                // todo this should happen very critically but should we kill the queue because of it?
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Failed,
                    new CallQueueLog {
                        Message = $"No backend server available for region {callQueueData.RegionId}. {serverSelectionResult.Message}",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            RegionData? regionDetails = await _regionManager.GetRegionById(callQueueData.RegionId);
            if (regionDetails == null)
            {
                _logger.LogError("Region details not found for {RegionId} during call {QueueId} processing.", callQueueData.RegionId, callQueueData.Id);
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Failed,
                    new CallQueueLog {
                        Message = $"System error: Region details for {callQueueData.RegionId} not found.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
                return;
            }

            bool successfullyForwarded = false;
            bool shouldRequeCall = false;
            foreach (var optimalServer in serverSelectionResult.Data)
            {
                RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Endpoint == optimalServer.ServerEndpoint && s.Type == ServerTypeEnum.Backend);
                if (backendServerDetails == null)
                {
                    await OnUpdateCallQueueStatusAndSendCampaignAction(
                        callQueueData,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLog {
                            Message = $"System error: Region details for {callQueueData.RegionId} not found.",
                            Type = CallQueueLogTypeEnum.Error
                        },
                        completedAt: DateTime.UtcNow
                    );
                    continue;
                }

                var requestDto = new BackendOutboundCallRequest
                {
                    QueueId = callQueueData.Id
                };

                var forwardResponse = await ForwardToBackendAsync(backendServerDetails, requestDto);
                if (!forwardResponse.Success)
                {
                    await OnUpdateCallQueueStatusAndSendCampaignAction(
                        callQueueData,
                        CallQueueStatusEnum.Failed,
                        new CallQueueLog
                        {
                            Message = $"Failed to forward to backend. [{forwardResponse.Code}] {forwardResponse.Message}",
                            Type = CallQueueLogTypeEnum.Error
                        },
                        completedAt: DateTime.UtcNow
                    );
                    break;
                }
                else
                {
                    var backendResult = forwardResponse.Data!;

                    if (!backendResult.Success)
                    {
                        successfullyForwarded = false;
                        shouldRequeCall = backendResult.Data!.ShouldRequeue;

                        await OnUpdateCallQueueStatusAndSendCampaignAction(
                            callQueueData,
                            CallQueueStatusEnum.Failed,
                            new CallQueueLog
                            {
                                Message = $"Backend call processing failure: [{backendResult.Code}] {backendResult.Message}",
                                Type = CallQueueLogTypeEnum.Error
                            },
                            completedAt: DateTime.UtcNow
                        );
                    }
                    else
                    {
                        successfullyForwarded = true;
                        shouldRequeCall = false;
                    }

                    break;
                }
            }

            if (shouldRequeCall)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Queued,
                    new CallQueueLog
                    {
                        Message = "Requeuing call.",
                        Type = CallQueueLogTypeEnum.Information
                    }
                );
            }
            else if (!successfullyForwarded)
            {
                await OnUpdateCallQueueStatusAndSendCampaignAction(
                    callQueueData,
                    CallQueueStatusEnum.Failed,
                    new CallQueueLog
                    {
                        Message = "Failed to forward to backend server.",
                        Type = CallQueueLogTypeEnum.Error
                    },
                    completedAt: DateTime.UtcNow
                );
            }
        }

        private async Task<FunctionReturnResult<FunctionReturnResult<BackendInitiateOutboundCallResultModel>>> ForwardToBackendAsync(RegionServerData backendServer, BackendOutboundCallRequest requestDto)
        {
            var result = new FunctionReturnResult<FunctionReturnResult<BackendInitiateOutboundCallResultModel>>();
            string endpoint = (backendServer.UseSSL ? "https://" : "http://") + backendServer.Endpoint;

            var baseUri = new Uri(endpoint);
            baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/call/outbound");

            try
            {
                using var client = _httpClientFactory.CreateClient("OutboundCallForwardClient");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(backendServer.APIKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", backendServer.APIKey);
                }

                var jsonPayload = JsonSerializer.Serialize(requestDto, _camelCaseSerializationOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(baseUri, content);

                var responseContentString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return result.SetFailureResult($"ForwardToBackend:{response.StatusCode}", $"Backend returned error: {response.StatusCode}. Details: {responseContentString}");
                }

                FunctionReturnResult<BackendInitiateOutboundCallResultModel?>? backendResponse;
                try
                {
                    backendResponse = JsonSerializer.Deserialize<FunctionReturnResult<BackendInitiateOutboundCallResultModel?>?>(responseContentString, _camelCaseSerializationOptions);
                }
                catch (Exception ex)
                {
                    return result.SetFailureResult(
                        "ForwardToBackend:BACKEND_RESPONSE_PARSE_FAIL",
                        $"Backend failed to process or invalid response format. Exception: {ex.Message}, Response: {responseContentString}"
                    );
                }

                if (backendResponse == null || backendResponse.Data == null)
                {
                    return result.SetFailureResult(
                        "ForwardToBackend:BACKEND_RESPONSE_PARSED_BUT_NULL",
                        $"Backend returned null response. Response: {responseContentString}"
                    );
                }

                return result.SetSuccessResult(backendResponse!);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request exception while forwarding call {QueueId} to {EndpointUrl}.", requestDto.QueueId, baseUri.ToString());
                return result.SetFailureResult("ForwardToBackend:HttpRequestError", $"HTTP request error: {httpEx.Message}");
            }
            catch (TaskCanceledException tex)
            {
                return result.SetFailureResult("ForwardToBackend:Timeout", "Request to backend timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception while forwarding call {QueueId} to {EndpointUrl}.", requestDto.QueueId, baseUri.ToString());
                return result.SetFailureResult("ForwardToBackend:GenericError", $"Exception: {ex.Message}");
            }
        }
    
        public async Task OnUpdateCallQueueStatusAndSendCampaignAction(
            OutboundCallQueueData callQueueData, CallQueueStatusEnum newStatus,
            CallQueueLog? log = null,
            string? newProcessingServerId = null,
            DateTime? processingStartedAt = null,
            DateTime? completedAt = null,
            Dictionary<string, string>? providerMetadata = null,
            string? providerCallId = null)
        {
            await _outboundCallQueueRepo.UpdateCallStatusAsync(callQueueData.Id, newStatus, log, newProcessingServerId, processingStartedAt, completedAt, providerMetadata, providerCallId);

            // Run action in background
            var logMessage = "Unknown";
            if (log != null)
            {
                logMessage = $"[{log.Type.ToString()}] {log.Message}";
            }

            _ = _campaignActionExecutorService.SendOutboundCallQueueTelephonyCampaignAction(callQueueData.Id, logMessage);
        }
    }
}

using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Models.Server;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Managers.Server;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Call
{
    public class OutboundCallProcessingOrchestrator
    {
        private readonly ILogger<OutboundCallProcessingOrchestrator> _logger;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepo;
        private readonly BillingValidationManager _billingValidationManager;
        private readonly ServerSelectionManager _serverSelectionManager;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _camelCaseSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


        public OutboundCallProcessingOrchestrator(
            ILogger<OutboundCallProcessingOrchestrator> logger,
            OutboundCallQueueRepository outboundCallQueueRepo,
            BillingValidationManager billingValidationManager,
            ServerSelectionManager serverSelectionManager,
            RegionManager regionManager,
            IHttpClientFactory httpClientFactory
        )
        {
            _logger = logger;
            _outboundCallQueueRepo = outboundCallQueueRepo;
            _billingValidationManager = billingValidationManager;
            _serverSelectionManager = serverSelectionManager;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task ProcessCallAsync(OutboundCallQueueData call)
        {
            // todo also check if phone is available and can make call

            var validationResult = await _billingValidationManager.CheckCreditAndConcurrencyAsync(call.BusinessId, "outbound call");
            if (!validationResult.Success)
            {
                if (
                    !string.IsNullOrWhiteSpace(validationResult.Code)
                    &&
                    (validationResult.Code.Contains("USER_CONCURRENCY_LIMIT") || validationResult.Code.Contains("BUSINESS_CONCURRENCY_LIMIT"))
                )
                {
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.Queued, new CallQueueLog { Message = $"Validation failed (will retry): {validationResult.Message}", Type = CallQueueLogTypeEnum.Information });     
                }
                else
                {
                    await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Canceled, new CallQueueLog { Message = $"Validation failed: [{validationResult.Code}] {validationResult.Message}", Type = CallQueueLogTypeEnum.Error });
                }
                return;
            }

            await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.ProcessingProxy, new CallQueueLog { Message = $"Processing Queue within proxy", Type = CallQueueLogTypeEnum.Information });

            var serverSelectionResult = await _serverSelectionManager.SelectOptimalServerAsync(call.RegionId);
            if (!serverSelectionResult.Success || !serverSelectionResult.Data.Any())
            {
                // todo this should happen very critically but should we kill the queue because of it?
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed, new CallQueueLog { Message = $"No backend server available for region {call.RegionId}. {serverSelectionResult.Message}", Type = CallQueueLogTypeEnum.Error });
                return;
            }

            RegionData? regionDetails = await _regionManager.GetRegionById(call.RegionId);
            if (regionDetails == null)
            {
                _logger.LogError("Region details not found for {RegionId} during call {QueueId} processing.", call.RegionId, call.Id);
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed, new CallQueueLog { Message = $"System error: Region details for {call.RegionId} not found.", Type = CallQueueLogTypeEnum.Error });
                return;
            }

            bool successfullyForwarded = false;
            foreach (var optimalServer in serverSelectionResult.Data)
            {
                RegionServerData? backendServerDetails = regionDetails.Servers.FirstOrDefault(s => s.Endpoint == optimalServer.ServerEndpoint && s.Type == ServerTypeEnum.Backend);
                if (backendServerDetails == null)
                {
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.Failed, new CallQueueLog { Message = $"System error: Region details for {call.RegionId} not found.", Type = CallQueueLogTypeEnum.Error });
                    continue;
                }

                var requestDto = new BackendOutboundCallRequest
                {
                    QueueId = call.Id
                };

                var forwardResponse = await ForwardToBackendAsync(backendServerDetails, requestDto);
                if (forwardResponse.Success)
                {
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.ProcessingBackend, new CallQueueLog { Message = $"Forwarded to backend {backendServerDetails.Endpoint}. Waiting for processing.", Type = CallQueueLogTypeEnum.Information }, newProcessingServerId: backendServerDetails.Endpoint);
                    successfullyForwarded = true;
                    break;
                }
                else
                {
                    // todo... should try the same server 3 times atleast??
                    await _outboundCallQueueRepo.UpdateCallStatusAsync(call.Id, CallQueueStatusEnum.Failed, new CallQueueLog { Message = $"Failed to forward to backend {backendServerDetails.Endpoint}. {forwardResponse.Message}", Type = CallQueueLogTypeEnum.Error });
                }
            }

            if (!successfullyForwarded)
            {
                await _outboundCallQueueRepo.MoveToArchivedAsync(call.Id, CallQueueStatusEnum.Failed, new CallQueueLog { Message = "Failed to forward to any backend server.", Type = CallQueueLogTypeEnum.Error });
            }
        }

        private async Task<FunctionReturnResult> ForwardToBackendAsync(RegionServerData backendServer, BackendOutboundCallRequest requestDto)
        {
            var result = new FunctionReturnResult();
            string endpoint = backendServer.UseSSL ? "https://" : "http://";
            endpoint += backendServer.Endpoint;
            string apiPath = "/api/call/outbound";

            try
            {
                using var client = _httpClientFactory.CreateClient("OutboundCallForwardClient");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(backendServer.APIKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", backendServer.APIKey);
                }

                var jsonPayload = JsonSerializer.Serialize(requestDto, _camelCaseSerializationOptions);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(new Uri(new Uri(endpoint), apiPath), content);

                var responseContentString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return result.SetFailureResult($"ForwardToBackend:{response.StatusCode}", $"Backend returned error: {response.StatusCode}. Details: {responseContentString}");
                }

                var backendResponse = JsonSerializer.Deserialize<FunctionReturnResult>(responseContentString, _camelCaseSerializationOptions);
                if (backendResponse == null || !backendResponse.Success)
                {
                    return result.SetFailureResult(backendResponse?.Code ?? "BackendParseFail", backendResponse?.Message ?? "Backend failed to process or invalid response format.");
                }

                return result.SetSuccessResult();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request exception while forwarding call {QueueId} to {EndpointUrl}{ApiPath}.", requestDto.QueueId, endpoint, apiPath);
                return result.SetFailureResult("ForwardToBackend:HttpRequestError", $"HTTP request error: {httpEx.Message}");
            }
            catch (TaskCanceledException tex)
            {
                return result.SetFailureResult("ForwardToBackend:Timeout", "Request to backend timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generic exception while forwarding call {QueueId} to {EndpointUrl}{ApiPath}.", requestDto.QueueId, endpoint, apiPath);
                return result.SetFailureResult("ForwardToBackend:GenericError", $"Exception: {ex.Message}");
            }
        }
    }
}

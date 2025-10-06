using IqraCore.Entities.Call.Queue;
using IqraCore.Entities.Helper.Call.Queue;
using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Models.Server;
using IqraCore.Models.Telephony;
using IqraInfrastructure.Managers.Region;
using IqraInfrastructure.Repositories.Call;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Call.Proxy
{
    public class CallStatusService
    {
        private readonly ILogger<CallStatusService> _logger;
        private readonly InboundCallQueueRepository _inboundCallQueueRepository;
        private readonly OutboundCallQueueRepository _outboundCallQueueRepository;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public CallStatusService(ILogger<CallStatusService> logger, InboundCallQueueRepository inboundCallQueueRepository, OutboundCallQueueRepository outboundCallQueueRepository, RegionManager regionManager, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _inboundCallQueueRepository = inboundCallQueueRepository;
            _outboundCallQueueRepository = outboundCallQueueRepository;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        /**
         * 
         * 
         * Incoming Calls 
         * 
         * 
        **/

        public async Task<FunctionReturnResult> NotifyInboundCallStarted(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();

            try
            {
                var callQueueWithSession = await GetIncomingCallQueueWithSession(telephonyWebhookContextModel);
                if (!callQueueWithSession.Success)
                {
                    return result.SetFailureResult("NotifyInboundCallStarted:" + callQueueWithSession.Code, callQueueWithSession.Message);
                }

                var regionData = await _regionManager.GetRegionById(callQueueWithSession.Data.RegionId);
                if (regionData == null)
                {
                    _logger.LogWarning("Region not found: {RegionId}", callQueueWithSession.Data.RegionId);
                    return result.SetFailureResult("NotifyInboundCallStarted:REGION_NOT_FOUND", "Region not found");
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueueWithSession.Data.ProcessingBackendServerId);
                if (regionServerData == null)
                {
                    _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueueWithSession.Data.ProcessingBackendServerId);
                    return result.SetFailureResult("NotifyInboundCallStarted:REGION_SERVER_NOT_FOUND", "Region server not found");
                }

                var forwardToBackendResult = await ForwardTelephonyClientStatusToBackendSession(regionServerData, "in-progress", callQueueWithSession.Data.SessionId, telephonyWebhookContextModel.Provider, telephonyWebhookContextModel.PhoneNumberId);
                if (!forwardToBackendResult.Success)
                {
                    return result.SetFailureResult("NotifyInboundCallStarted:" + forwardToBackendResult.Code, forwardToBackendResult.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex) {
                return result.SetFailureResult("NotifyInboundCallStarted:EXCEPTION", ex.Message);
            }
        }

        public async Task<FunctionReturnResult> NotifyInboundCallEnded(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();

            try
            {
                var callQueueWithSession = await GetIncomingCallQueueWithSession(telephonyWebhookContextModel);
                if (!callQueueWithSession.Success)
                {
                    return result.SetFailureResult("NotifyInboundCallStarted:" + callQueueWithSession.Code, callQueueWithSession.Message);
                }

                var regionData = await _regionManager.GetRegionById(callQueueWithSession.Data.RegionId);
                if (regionData == null)
                {
                    _logger.LogWarning("Region not found: {RegionId}", callQueueWithSession.Data.RegionId);
                    return result.SetFailureResult("NotifyInboundCallStarted:REGION_NOT_FOUND", "Region not found");
                }
                var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueueWithSession.Data.ProcessingBackendServerId);
                if (regionServerData == null)
                {
                    _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueueWithSession.Data.ProcessingBackendServerId);
                    return result.SetFailureResult("NotifyInboundCallStarted:REGION_SERVER_NOT_FOUND", "Region server not found");
                }

                var forwardToBackendResult = await ForwardTelephonyClientStatusToBackendSession(regionServerData, "completed", callQueueWithSession.Data.SessionId, telephonyWebhookContextModel.Provider, telephonyWebhookContextModel.PhoneNumberId);
                if (!forwardToBackendResult.Success)
                {
                    return result.SetFailureResult("NotifyInboundCallStarted:" + forwardToBackendResult.Code, forwardToBackendResult.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("NotifyInboundCallStarted:EXCEPTION", ex.Message);
            }
        }

        private async Task<FunctionReturnResult<InboundCallQueueData?>> GetIncomingCallQueueWithSession(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult<InboundCallQueueData?>();

            try
            {
                var callQueue = await _inboundCallQueueRepository.GetInboundCallQueueByProviderCallIdAsync(telephonyWebhookContextModel.Provider, telephonyWebhookContextModel.CallId, telephonyWebhookContextModel.BusinessId, telephonyWebhookContextModel.PhoneNumberId);
                if (callQueue == null)
                {
                    return result.SetFailureResult("GetIncomingCallSessionId:NOT_FOUND", "Call not found in queue");
                }

                if (string.IsNullOrWhiteSpace(callQueue.SessionId) || callQueue.Status != CallQueueStatusEnum.ProcessedBackend)
                {
                    return result.SetFailureResult("GetIncomingCallSessionId:INVALID_STATUS", "Call has no session id or status is not processing backend");
                }

                return result.SetSuccessResult(callQueue);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetIncomingCallSessionId:EXCEPTION", ex.Message);
            }
        }

        /**
         * 
         * 
         * Outbound Calls 
         * 
         * 
        **/

        public async Task<FunctionReturnResult> NotifyOutboundCallStatus(string sessionId, TelephonyWebhookContextModel telephonyWebhookContextModel, string status)
        {
            var result = new FunctionReturnResult();

            var callQueueWithSession = await GetOutboundCallQueueWithSession(sessionId);
            if (!callQueueWithSession.Success)
            {
                return result.SetFailureResult("NotifyOutboundCallStatus:" + callQueueWithSession.Code, callQueueWithSession.Message);
            }

            var regionData = await _regionManager.GetRegionById(callQueueWithSession.Data.RegionId);
            if (regionData == null)
            {
                _logger.LogWarning("Region not found: {RegionId}", callQueueWithSession.Data.RegionId);
                return result.SetFailureResult("NotifyOutboundCallStatus:REGION_NOT_FOUND", "Region not found");
            }
            var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueueWithSession.Data.ProcessingBackendServerId);
            if (regionServerData == null)
            {
                _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueueWithSession.Data.ProcessingBackendServerId);
                return result.SetFailureResult("NotifyOutboundCallStatus:REGION_SERVER_NOT_FOUND", "Region server not found");
            }

            var forwardToBackendResult = await ForwardTelephonyClientStatusToBackendSession(regionServerData, status, callQueueWithSession.Data.SessionId, telephonyWebhookContextModel.Provider, telephonyWebhookContextModel.PhoneNumberId);
            if (!forwardToBackendResult.Success)
            {
                return result.SetFailureResult("NotifyOutboundCallStatus:" + forwardToBackendResult.Code, forwardToBackendResult.Message);
            }

            return result.SetSuccessResult();
        }

        private async Task<FunctionReturnResult<OutboundCallQueueData?>> GetOutboundCallQueueWithSession(string sessionId)
        {
            var result = new FunctionReturnResult<OutboundCallQueueData?>();

            try
            {
                var callQueue = await _outboundCallQueueRepository.GetOutboundCallQueueBySessionIdAsync(sessionId);
                if (callQueue == null)
                {
                    return result.SetFailureResult("GetOutboundCallQueueWithSession:NOT_FOUND", "Call not found in queue");
                }

                if (string.IsNullOrWhiteSpace(callQueue.SessionId) || callQueue.Status != CallQueueStatusEnum.ProcessedBackend)
                {
                    return result.SetFailureResult("GetOutboundCallQueueWithSession:INVALID_STATUS", "Call has no session id or status is not processing backend");
                }

                return result.SetSuccessResult(callQueue);
            }
            catch (Exception ex)
            {
                return result.SetFailureResult("GetOutboundCallQueueWithSession:EXCEPTION", ex.Message);
            }
        }

        // Common helpers

        private async Task<FunctionReturnResult> ForwardTelephonyClientStatusToBackendSession(RegionServerData serverData, string status, string sessionId, TelephonyProviderEnum provider, string phoneNumberId)
        {
            var result = new FunctionReturnResult();

            try
            {
                // Create the HttpClient
                using var client = _httpClientFactory.CreateClient("CallStatusManagerForward");

                // Set headers
                client.Timeout = TimeSpan.FromSeconds(30); // check if 10 seconds is good
                client.DefaultRequestHeaders.Add("X-API-Key", serverData.APIKey);

                // Prepare the request body
                var requestBody = new TelephonyStatusNotifyToBackendModel()
                {
                    Provider = provider,
                    PhoneNumberId = phoneNumberId,
                    Status = status
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                // Send the notification
                string serverEndpoint = serverData.Endpoint;
                if (serverData.UseSSL)
                {
                    serverEndpoint = "https://" + serverEndpoint;
                }
                else
                {
                    serverEndpoint = "http://" + serverEndpoint;
                }

                var baseUri = new Uri(serverEndpoint);
                baseUri = new Uri(baseUri, $"{(baseUri.AbsolutePath != "/" ? baseUri.AbsolutePath : "")}/api/call/{sessionId}/telephonyclient/status");
                var response = await client.PostAsync(baseUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogError("Failed to notify backend of call status: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    return result.SetFailureResult($"ForwardSessionStatusToBackend:STATUS_CODE_{response.StatusCode}", errorContent);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent);
                if (responseData == null) // should never hapopen tho
                {
                    _logger.LogError("Invalid response from backend server {ResponseContent}", responseContent);

                    return result.SetFailureResult("ForwardSessionStatusToBackend:INVALID_RESPONSE", responseContent);
                }

                if (!responseData.Success)
                {
                    _logger.LogError("Error forwarding call status notificaiton to backend server: {Code} - {Message}", responseData.Code, responseData.Message);

                    return result.SetFailureResult($"ForwardSessionStatusToBackend:{responseData.Code}", responseData.Message);
                }

                return result.SetSuccessResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying backend of call status");

                return result.SetFailureResult("ForwardSessionStatusToBackend:EXCEPTION", ex.Message);
            }
        }

    }
}

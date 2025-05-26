using IqraCore.Entities.Helper.Telephony;
using IqraCore.Entities.Helpers;
using IqraCore.Models.Telephony;

namespace IqraInfrastructure.Managers.Call
{
    public class CallStatusManager
    {
        public async Task<FunctionReturnResult> NotifyCallRinging(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallBusy(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallStarted(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {
            var result = new FunctionReturnResult();




            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> NotifyCallEnded(TelephonyWebhookContextModel telephonyWebhookContextModel)
        {

            var result = new FunctionReturnResult();




            return result.SetSuccessResult();


            //try
            //{
            //    // Find the call in the queue
            //    var callQueue = await _inboundCallQueueRepository.GetInboundCallQueueByProviderCallIdAsync(provider, callId, businessId, phoneNumberId);
            //    if (callQueue == null)
            //    {
            //        _logger.LogWarning("Call not found in queue for end notification: {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
            //        return;
            //    }

            //    // If the call has a session ID, notify the backend app
            //    if (!string.IsNullOrEmpty(callQueue.SessionId) && !string.IsNullOrEmpty(callQueue.ProcessingBackendServerId))
            //    {
            //        // Get Region Api key
            //        var regionData = await _regionManager.GetRegionById(callQueue.RegionId);
            //        if (regionData == null)
            //        {
            //            _logger.LogWarning("Region not found: {RegionId}", callQueue.RegionId);
            //            return;
            //        }
            //        var regionServerData = regionData.Servers.FirstOrDefault(s => s.Endpoint == callQueue.ProcessingBackendServerId);
            //        if (regionServerData == null)
            //        {
            //            _logger.LogWarning("Region server not found: {ServerEndpoint}", callQueue.ProcessingBackendServerId);
            //            return;
            //        }
            //        var regionServerApiKey = regionServerData.APIKey;
            //        var regionUseSSL = regionServerData.UseSSL;

            //        await NotifyBackendCallEndedAsync(callQueue.ProcessingBackendServerId, regionServerApiKey, regionUseSSL, callQueue.SessionId, provider, phoneNumberId);
            //        // todo notify backend and get success response to try again
            //    }

            //    _logger.LogInformation("Call {CallId} marked as ended", callQueue.Id);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error processing call end notification for {CallId} for provider {Provider} in {businessId}/{phoneNumberId}", callId, provider, businessId, phoneNumberId);
            //}
        }

        private async Task NotifyBackendCallEndedAsync(string serverEndpoint, string apiKey, bool regionUseSSL, string sessionId, TelephonyProviderEnum provider, string phoneNumberId)
        {
            //try
            //{
            //    // Create the HttpClient
            //    using var client = _httpClientFactory.CreateClient("CallManagerServerForward");

            //    // Set headers
            //    client.Timeout = TimeSpan.FromSeconds(15); // todo check if 15seconds is good
            //    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            //    // Prepare the request body
            //    var requestBody = new CallEndNotifyBackendData()
            //    {
            //        Provider = provider,
            //        PhoneNumberId = phoneNumberId
            //    };

            //    var jsonContent = JsonSerializer.Serialize(requestBody);
            //    var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            //    // Send the notification
            //    if (regionUseSSL)
            //    {
            //        serverEndpoint = "https://" + serverEndpoint;
            //    }
            //    else
            //    {
            //        serverEndpoint = "http://" + serverEndpoint;
            //    }

            //    var baseUri = new Uri(serverEndpoint);
            //    baseUri = new Uri(baseUri, $"/api/call/{sessionId}/ended");
            //    var response = await client.PostAsync(baseUri, content);

            //    if (!response.IsSuccessStatusCode)
            //    {
            //        var errorContent = await response.Content.ReadAsStringAsync();

            //        _logger.LogError("Failed to notify backend of call end: {StatusCode} - {Error}", response.StatusCode, errorContent);

            //        return;
            //    }

            //    var responseContent = await response.Content.ReadAsStringAsync();
            //    var responseData = JsonSerializer.Deserialize<FunctionReturnResult>(responseContent, _seralizationOptionCamelCase);
            //    if (responseData == null) // should never hapopen tho
            //    {
            //        _logger.LogError("Invalid response from backend server {ResponseContent}", responseContent);

            //        return;
            //    }

            //    if (!responseData.Success)
            //    {
            //        _logger.LogError("Error forwarding call ended notificaiton to backend server: {Code} - {Message}", responseData.Code, responseData.Message);

            //        return;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error notifying backend of call end");
            //}
        }

    }
}

using IqraCore.Entities.Helpers;
using IqraCore.Entities.Telephony.Telnyx;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.Telephony
{
    public class TelnyxManager
    {
        private readonly ILogger<TelnyxManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public TelnyxManager(ILogger<TelnyxManager> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            // Configure JsonSerializer for Telnyx's snake_case naming convention
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private HttpClient CreateConfiguredHttpClient(string apiKey)
        {
            var client = _httpClientFactory.CreateClient("TelnyxClient"); // Assumes a client configured with the Telnyx base URL
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<FunctionReturnResult<TelnyxDialResponseData>> MakeCallAsync(string apiKey, string from, string to, string connectionId, string webhookUrl, string streamUrl)
        {
            var result = new FunctionReturnResult<TelnyxDialResponseData>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var dialRequest = new
                    {
                        To = to,
                        From = from,
                        ConnectionId = connectionId,
                        WebhookUrl = webhookUrl,
                        StreamUrl = streamUrl,
                        StreamTrack = "both_tracks",
                        StreamCodex = "PCMA",
                        StreamBidirectionalMode = "rtp",
                        StreamBidirectionalCodec = "PCMA",
                        StreamBidirectionalSamplingRate = 8000
                    };

                    string jsonPayload = JsonSerializer.Serialize(dialRequest, _jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("v2/calls", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "MakeCall:1";
                        result.Message = $"Error making call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Telnyx API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var callResponse = JsonSerializer.Deserialize<TelnyxDialResponse>(responseContent, _jsonOptions);

                    if (callResponse?.Data == null)
                    {
                        result.Code = "MakeCall:2";
                        result.Message = "Failed to deserialize call response from Telnyx.";
                        return result;
                    }

                    result.Success = true;
                    result.Data = callResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "MakeCall:3";
                result.Message = $"Exception making call: {ex.Message}";
                _logger.LogError(ex, "Exception in TelnyxManager.MakeCallAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult<bool>> EndCallAsync(string apiKey, string callControlId)
        {
            var result = new FunctionReturnResult<bool>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    // Telnyx requires an empty JSON body for this POST request
                    var content = new StringContent("{}", Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"v2/calls/{callControlId}/actions/hangup", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "EndCall:1";
                        result.Message = $"Error ending call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Telnyx API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    result.Success = true;
                    result.Data = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "EndCall:2";
                result.Message = $"Exception ending call: {ex.Message}";
                _logger.LogError(ex, "Exception in TelnyxManager.EndCallAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<TelnyxAvailablePhoneNumber>>> SearchAvailableNumbersAsync(string apiKey, string countryCode, string startsWith = null)
        {
            var result = new FunctionReturnResult<List<TelnyxAvailablePhoneNumber>>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    string requestUri = $"v2/available_phone_numbers?filter[country_code]={countryCode}&filter[features][]=voice";
                    if (!string.IsNullOrEmpty(startsWith))
                    {
                        requestUri += $"&filter[phone_number][starts_with]={startsWith}";
                    }

                    var response = await client.GetAsync(requestUri);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SearchAvailableNumbers:1";
                        result.Message = $"Error searching for numbers: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Telnyx API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var numbersResponse = JsonSerializer.Deserialize<TelnyxAvailablePhoneNumbersResponse>(responseContent, _jsonOptions);

                    if (numbersResponse?.Data == null)
                    {
                        result.Code = "SearchAvailableNumbers:2";
                        result.Message = "Failed to deserialize available numbers response from Telnyx.";
                        return result;
                    }

                    result.Success = true;
                    result.Data = numbersResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "SearchAvailableNumbers:3";
                result.Message = $"Exception searching for numbers: {ex.Message}";
                _logger.LogError(ex, "Exception in TelnyxManager.SearchAvailableNumbersAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult> SendDtmfAsync(string apiKey, string callControlId, string digits)
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var sendDtmfRequest = new { digits };
                    string jsonPayload = JsonSerializer.Serialize(sendDtmfRequest, _jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"v2/calls/{callControlId}/actions/send_dtmf", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SendDtmfAsync:1";
                        result.Message = $"Error sending DTMF: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Telnyx API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    return result.SetSuccessResult();
                }
            }
            catch (Exception ex)
            {
                result.Code = "SendDtmfAsync:3";
                result.Message = $"Exception in SendDtmfAsync: {ex.Message}";
                _logger.LogError(ex, "Exception in TelnyxManager.SendDtmfAsync");
            }

            return result;
        }
    }
}

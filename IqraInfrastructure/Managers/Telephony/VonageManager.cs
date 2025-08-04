
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Telephony.Vonage;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IqraInfrastructure.Managers.Telephony
{
    public class VonageManager
    {
        private readonly ILogger<VonageManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public VonageManager(ILogger<VonageManager> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // --- HTTP Client Helpers for Dual Authentication ---

        // Client for the Calls API (api.nexmo.com), which uses JWT Bearer tokens.
        private HttpClient CreateApiHttpClient(string jwt)
        {
            var client = _httpClientFactory.CreateClient("VonageApiClient");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        // Client for the Numbers API (rest.nexmo.com), which uses Basic Auth.
        private HttpClient CreateRestHttpClient(string apiKey, string apiSecret)
        {
            var client = _httpClientFactory.CreateClient("VonageRestClient");
            client.DefaultRequestHeaders.Clear();
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        // --- API Methods ---

        public async Task<FunctionReturnResult<VonageCallResponse>> MakeCallAsync(string jwt, string from, string to, string eventUrl, string websocketUrl)
        {
            var result = new FunctionReturnResult<VonageCallResponse>();

            try
            {
                using (var client = CreateApiHttpClient(jwt))
                {
                    var ncco = new[]
                    {
                        new
                        {
                            Action = "connect",
                            Endpoint = new[]
                            {
                                new
                                {
                                    Type = "websocket",
                                    Uri = websocketUrl,
                                    ContentType = "audio/l16;rate=8000"
                                }
                            }
                        }
                    };

                    var callRequest = new
                    {
                        To = new[] { new { Type = "phone", Number = to } },
                        From = new { Type = "phone", Number = from },
                        Ncco = ncco,
                        EventUrl = new[] { eventUrl }
                    };

                    string jsonPayload = JsonSerializer.Serialize(callRequest, _jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("v1/calls", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "MakeCall:1";
                        result.Message = $"Error making call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Vonage API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var callResponse = JsonSerializer.Deserialize<VonageCallResponse>(responseContent, _jsonOptions);

                    if (callResponse?.Uuid == null)
                    {
                        result.Code = "MakeCall:2";
                        result.Message = "Failed to deserialize call response from Vonage.";
                        return result;
                    }

                    result.Success = true;
                    result.Data = callResponse;
                }
            }
            catch (Exception ex)
            {
                result.Code = "MakeCall:3";
                result.Message = $"Exception making call: {ex.Message}";
                _logger.LogError(ex, "Exception in VonageManager.MakeCallAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult<bool>> EndCallAsync(string jwt, string callUuid)
        {
            var result = new FunctionReturnResult<bool>();

            try
            {
                using (var client = CreateApiHttpClient(jwt))
                {
                    var hangupRequest = new { Action = "hangup" };
                    string jsonPayload = JsonSerializer.Serialize(hangupRequest, _jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // Vonage uses a PUT request to modify/end a call.
                    var response = await client.PutAsync($"v1/calls/{callUuid}", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "EndCall:1";
                        result.Message = $"Error ending call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Vonage API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
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
                _logger.LogError(ex, "Exception in VonageManager.EndCallAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult<VonageNumbersResponse>> SearchAvailableNumbersAsync(string apiKey, string apiSecret, string countryCode)
        {
            var result = new FunctionReturnResult<VonageNumbersResponse>();

            try
            {
                using (var client = CreateRestHttpClient(apiKey, apiSecret))
                {
                    string requestUri = $"number/search?country={countryCode}&features=VOICE&size=20";

                    var response = await client.GetAsync(requestUri);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SearchAvailableNumbers:1";
                        result.Message = $"Error searching for numbers: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Vonage API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var numbersResponse = JsonSerializer.Deserialize<VonageNumbersResponse>(responseContent, _jsonOptions);

                    if (numbersResponse?.Numbers == null)
                    {
                        result.Code = "SearchAvailableNumbers:2";
                        result.Message = "Failed to deserialize available numbers response from Vonage.";
                        return result;
                    }

                    result.Success = true;
                    result.Data = numbersResponse;
                }
            }
            catch (Exception ex)
            {
                result.Code = "SearchAvailableNumbers:3";
                result.Message = $"Exception searching for numbers: {ex.Message}";
                _logger.LogError(ex, "Exception in VonageManager.SearchAvailableNumbersAsync");
            }

            return result;
        }

        public async Task<FunctionReturnResult> SendDtmfAsync(string jwt, string callUuid, string digits)
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateApiHttpClient(jwt))
                {
                    var dtmfRequest = new { digits };
                    string jsonPayload = JsonSerializer.Serialize(dtmfRequest, _jsonOptions);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync($"v1/calls/{callUuid}/dtmf", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SendDtmf:1";
                        result.Message = $"Error sending DTMF tones: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Vonage API error sending DTMF: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    return result.SetSuccessResult();
                }
            }
            catch (Exception ex)
            {
                result.Code = "SendDtmf:2";
                result.Message = $"Exception sending DTMF tones: {ex.Message}";
                _logger.LogError(ex, "Exception in VonageManager.SendDtmfAsync");
            }

            return result;
        }
    }
}

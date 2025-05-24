using IqraCore.Entities.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using IqraCore.Entities.Telephony.ModemTel;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.Telephony
{
    public class ModemTelManager
    {
        private readonly ILogger<ModemTelManager> _logger;

        private readonly IHttpClientFactory _httpClientFactory;      
        private readonly JsonSerializerOptions _jsonOptions;

        public ModemTelManager(ILogger<ModemTelManager> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;

            _httpClientFactory = httpClientFactory;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        private HttpClient CreateConfiguredHttpClient(string apiKey)
        {
            var client = _httpClientFactory.CreateClient("ModemTelClient");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public async Task<FunctionReturnResult<List<ModemTelPhoneNumber>>> GetPhoneNumbersAsync(string apiKey, string apiBaseUrl)
        {
            var result = new FunctionReturnResult<List<ModemTelPhoneNumber>>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var response = await client.GetAsync($"{apiBaseUrl}/api/v1/numbers");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumbers:1";
                        result.Message = $"Error getting phone numbers: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<List<ModemTelPhoneNumber>>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "GetPhoneNumbers:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumbers:3";
                result.Message = $"Error retrieving phone numbers: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelPhoneNumberDetails>> GetPhoneNumberDetailsAsync(string apiKey, string apiBaseUrl, string phoneNumberId)
        {
            var result = new FunctionReturnResult<ModemTelPhoneNumberDetails>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var baseUri = new Uri(apiBaseUrl);
                    baseUri = new Uri(baseUri, $"/api/v1/numbers/{phoneNumberId}");

                    var response = await client.GetAsync(baseUri);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumberDetails:1";
                        result.Message = $"Error getting phone number details: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelPhoneNumberDetails>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "GetPhoneNumberDetails:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumberDetails:3";
                result.Message = $"Error retrieving phone number details: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelPhoneNumberDetails>> GetPhoneNumberByCountryCodeAndNumberAsync(string apiKey, string apiBaseUrl, string countryCode, string number)
        {
            var result = new FunctionReturnResult<ModemTelPhoneNumberDetails>();

            try
            {
                // Validate input parameters
                if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(number))
                {
                    result.Code = "GetPhoneNumberByCountryCodeAndNumber:1";
                    result.Message = "Country code and number are required";
                    return result;
                }

                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    Uri baseURI = new Uri(apiBaseUrl);
                    baseURI = new Uri(baseURI, $"/api/v1/numbers/{countryCode}-{number}");
                    var response = await client.GetAsync(baseURI);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumberByCountryCodeAndNumber:2";
                        result.Message = $"Error getting phone number: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelPhoneNumberDetails>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "GetPhoneNumberByCountryCodeAndNumber:3";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumberByCountryCodeAndNumber:4";
                result.Message = $"Error retrieving phone number details: {ex.Message}";
                return result;
            }
        }

        public async Task<FunctionReturnResult> ValidatePhoneNumberAsync(string apiKey, string apiBaseUrl, string modemTelPhoneNumberId, bool requireCallCapability = true, bool requireSmsCapability = false)
        {
            var result = new FunctionReturnResult();

            try
            {
                var phoneNumberResult = await GetPhoneNumberDetailsAsync(apiKey, apiBaseUrl, modemTelPhoneNumberId);

                if (!phoneNumberResult.Success)
                {
                    result.Code = "ValidatePhoneNumber:1";
                    result.Message = phoneNumberResult.Message;
                    return result;
                }

                var phoneNumber = phoneNumberResult.Data;

                if (requireCallCapability && !phoneNumber.CanMakeCalls)
                {
                    result.Code = "ValidatePhoneNumber:2";
                    result.Message = "The phone number does not support making calls";
                    return result;
                }

                if (requireSmsCapability && !phoneNumber.CanSendSms)
                {
                    result.Code = "ValidatePhoneNumber:3";
                    result.Message = "The phone number does not support sending SMS";
                    return result;
                }

                if (!phoneNumber.IsActive)
                {
                    result.Code = "ValidatePhoneNumber:4";
                    result.Message = "The phone number is not active";
                    return result;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Code = "ValidatePhoneNumber:5";
                result.Message = $"Error validating phone number: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult> UpdateWebhookUrlAsync(string apiKey, string apiBaseUrl, string phoneNumberId, string webhookUrl)
        {
            var result = new FunctionReturnResult();

            try
            {
                // The ModemTel API doesn't have a direct endpoint to update webhook URL in the provided OpenAPI spec
                // We'll need to implement this when the endpoint becomes available
                // For now, we'll just return a message indicating this limitation

                result.Code = "UpdateWebhookUrl:1";
                result.Message = "Webhook URL update not supported by the ModemTel API at this time";

                // When the API supports this functionality, it would look something like:
                /*
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var updateData = new { webhookUrl = webhookUrl };
                    var jsonContent = JsonSerializer.Serialize(updateData, _jsonOptions);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    var response = await client.PutAsync($"{apiBaseUrl}/api/v1/numbers/{phoneNumberId}/webhook", httpContent);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "UpdateWebhookUrl:1";
                        result.Message = $"Error updating webhook URL: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }
                    
                    result.Success = true;
                    result.Data = true;
                }
                */
            }
            catch (Exception ex)
            {
                result.Code = "UpdateWebhookUrl:2";
                result.Message = $"Error updating webhook URL: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelCall>> MakeCallAsync(string apiKey, string apiBaseUrl, string phoneNumberId, string toNumber, string statusCallbackUrl, string websocketUrl, string websocketToken)
        {
            var result = new FunctionReturnResult<ModemTelCall>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var callRequest = new ModemTelCallRequest
                    {
                        PhoneNumberId = phoneNumberId,
                        To = toNumber,
                        StatusCallback = statusCallbackUrl,
                        StreamToken = websocketToken,
                        StreamUrl = websocketUrl,
                    };

                    var jsonContent = JsonSerializer.Serialize(callRequest, _jsonOptions);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{apiBaseUrl}/api/v1/calls", httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "MakeCall:1";
                        result.Message = $"Error making outbound call: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelCall>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "MakeCall:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "MakeCall:3";
                result.Message = $"Error making outbound call: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelMessage>> SendSmsAsync(string apiKey, string apiBaseUrl, string phoneNumberId, string toNumber, string messageBody)
        {
            var result = new FunctionReturnResult<ModemTelMessage>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var messageRequest = new ModemTelMessageRequest
                    {
                        PhoneNumberId = phoneNumberId,
                        To = toNumber,
                        Body = messageBody
                    };

                    var jsonContent = JsonSerializer.Serialize(messageRequest, _jsonOptions);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{apiBaseUrl}/api/v1/messages", httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SendSms:1";
                        result.Message = $"Error sending SMS message: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelMessage>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "SendSms:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "SendSms:3";
                result.Message = $"Error sending SMS message: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelCall>> GetCallDetailsAsync(string apiKey, string apiBaseUrl, string callId)
        {
            var result = new FunctionReturnResult<ModemTelCall>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var response = await client.GetAsync($"{apiBaseUrl}/api/v1/calls/{callId}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetCallDetails:1";
                        result.Message = $"Error getting call details: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelCall>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "GetCallDetails:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetCallDetails:3";
                result.Message = $"Error retrieving call details: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult> HangupCallAsync(string apiKey, string apiBaseUrl, string callId)
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    Uri baseURI = new Uri(apiBaseUrl);
                    baseURI = new Uri(baseURI, $"/api/v1/calls/{callId}/hangup");
                    var response = await client.PostAsync(baseURI, null);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "HangupCall:1";
                        result.Message = $"Error hanging up call: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "HangupCall:2";
                result.Message = $"Error hanging up call: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult> SendDtmfAsync(string apiKey, string apiBaseUrl, string callId, string digits)
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var dtmfRequest = new ModemTelDtmfSendModel
                    {
                        Digits = digits
                    };

                    var jsonContent = JsonSerializer.Serialize(dtmfRequest, _jsonOptions);
                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{apiBaseUrl}/api/v1/calls/{callId}/dtmf", httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "SendDtmf:1";
                        result.Message = $"Error sending DTMF tones: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "SendDtmf:2";
                result.Message = $"Error sending DTMF tones: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult> AnswerCallAsync(string apiKey, string apiBaseUrl, string callId)
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var baseUri = new Uri(apiBaseUrl);
                    baseUri = new Uri(baseUri, $"/api/v1/calls/{callId}/answer");
                    var response = await client.PostAsync(baseUri, null);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "AnswerCall:1";
                        result.Message = $"Error answering call: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "AnswerCall:2";
                result.Message = $"Error answering call: {ex.Message}";
            }

            return result;
        }

        public async Task<FunctionReturnResult<ModemTelMediaSession>> GetCallMediaSessionAsync(string apiKey, string apiBaseUrl, string callId)
        {
            var result = new FunctionReturnResult<ModemTelMediaSession>();

            try
            {
                using (var client = CreateConfiguredHttpClient(apiKey))
                {
                    var response = await client.GetAsync($"{apiBaseUrl}/api/v1/calls/{callId}/media");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetCallMediaSession:1";
                        result.Message = $"Error getting call media session: {response.StatusCode}. Details: {errorContent}";
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<ModemTelResponse<ModemTelMediaSession>>(content, _jsonOptions);

                    if (apiResponse?.Data == null)
                    {
                        result.Code = "GetCallMediaSession:2";
                        result.Message = "Invalid response format received from ModemTel API";
                        return result;
                    }

                    result.Success = true;
                    result.Data = apiResponse.Data;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetCallMediaSession:3";
                result.Message = $"Error retrieving call media session: {ex.Message}";
            }

            return result;
        }
    }
}
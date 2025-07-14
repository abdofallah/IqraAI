using IqraCore.Entities.Helpers;
using IqraCore.Entities.Telephony.Twilio;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Twilio.TwiML.Voice;
using Twilio.TwiML;
using GenerativeAI.Core;

namespace IqraInfrastructure.Managers.Telephony
{
    public class TwilioManager
    {
        private readonly ILogger<TwilioManager> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly JsonSerializerOptions _jsonOptions;      

        public TwilioManager(ILogger<TwilioManager> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        private HttpClient CreateConfiguredHttpClient(string accountSid, string authToken)
        {
            var client = _httpClientFactory.CreateClient("TwilioClient");
            client.DefaultRequestHeaders.Clear();

            // Add basic authentication
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        public async Task<FunctionReturnResult<TwilioPhoneNumberDetails>> GetPhoneNumberDetailsAsync(string accountSid, string authToken, string phoneNumberId)
        {
            var result = new FunctionReturnResult<TwilioPhoneNumberDetails>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Get the phone number details
                    var response = await client.GetAsync($"Accounts/{accountSid}/IncomingPhoneNumbers/{phoneNumberId}.json");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumberDetails:1";
                        result.Message = $"Error getting phone number details: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var phoneNumber = JsonSerializer.Deserialize<TwilioPhoneNumberDetails>(content, _jsonOptions);

                    if (phoneNumber == null)
                    {
                        result.Code = "GetPhoneNumberDetails:2";
                        result.Message = "Failed to deserialize phone number details";
                        _logger.LogError("Failed to deserialize Twilio phone number response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = phoneNumber;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumberDetails:3";
                result.Message = $"Error retrieving phone number details: {ex.Message}";
                _logger.LogError(ex, "Error retrieving Twilio phone number details");
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<TwilioPhoneNumberDetails>>> GetPhoneNumbersByNumberAsync(string accountSid, string authToken, string countryCode, string phoneNumber)
        {
            var result = new FunctionReturnResult<List<TwilioPhoneNumberDetails>>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Get the list of phone numbers
                    var response = await client.GetAsync($"Accounts/{accountSid}/IncomingPhoneNumbers.json?PhoneNumber=\"%2B{countryCode}{phoneNumber}\"");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumbersByNumberAsync:1";
                        result.Message = $"Error getting phone numbers: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<TwilioPhoneNumberListResponse>(content, _jsonOptions);

                    if (responseData == null || responseData.IncomingPhoneNumbers == null)
                    {
                        result.Code = "GetPhoneNumbersByNumberAsync:2";
                        result.Message = "Failed to deserialize phone numbers list";
                        _logger.LogError("Failed to deserialize Twilio phone numbers response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = responseData.IncomingPhoneNumbers;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumbersByNumberAsync:3";
                result.Message = $"Error retrieving phone numbers: {ex.Message}";
                _logger.LogError(ex, "Error retrieving Twilio phone numbers");
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<TwilioPhoneNumberDetails>>> GetPhoneNumbersAsync(string accountSid, string authToken)
        {
            var result = new FunctionReturnResult<List<TwilioPhoneNumberDetails>>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Get the list of phone numbers
                    var response = await client.GetAsync($"Accounts/{accountSid}/IncomingPhoneNumbers.json");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetPhoneNumbers:1";
                        result.Message = $"Error getting phone numbers: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<TwilioPhoneNumberListResponse>(content, _jsonOptions);

                    if (responseData == null || responseData.IncomingPhoneNumbers == null)
                    {
                        result.Code = "GetPhoneNumbers:2";
                        result.Message = "Failed to deserialize phone numbers list";
                        _logger.LogError("Failed to deserialize Twilio phone numbers response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = responseData.IncomingPhoneNumbers;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetPhoneNumbers:3";
                result.Message = $"Error retrieving phone numbers: {ex.Message}";
                _logger.LogError(ex, "Error retrieving Twilio phone numbers");
            }

            return result;
        }

        public async Task<FunctionReturnResult<TwilioCallDetails>> MakeCallAsync(string accountSid, string authToken, string from, string to, string statusCallbackUrl, string websocketUrl)
        {
            var result = new FunctionReturnResult<TwilioCallDetails>();

            try
            {
                var voiceResponse = new VoiceResponse();
                var connect = new Connect();
                var stream = new Twilio.TwiML.Voice.Stream(url: websocketUrl);
                connect.Append(stream);
                voiceResponse.Append(connect);
                string twimlString = voiceResponse.ToString();

                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Prepare request body
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("From", from),
                        new KeyValuePair<string, string>("To", to),
                        new KeyValuePair<string, string>("Twiml", twimlString),
                        new KeyValuePair<string, string>("StatusCallback", statusCallbackUrl),
                        new KeyValuePair<string, string>("StatusCallbackMethod", "POST")
                    });

                    // Make outbound call
                    var response = await client.PostAsync($"Accounts/{accountSid}/Calls.json", formContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "MakeCall:1";
                        result.Message = $"Error making call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var callDetails = JsonSerializer.Deserialize<TwilioCallDetails>(content, _jsonOptions);

                    if (callDetails == null)
                    {
                        result.Code = "MakeCall:2";
                        result.Message = "Failed to deserialize call details";
                        _logger.LogError("Failed to deserialize Twilio call response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = callDetails;
                }
            }
            catch (Exception ex)
            {
                result.Code = "MakeCall:3";
                result.Message = $"Error making call: {ex.Message}";
                _logger.LogError(ex, "Error making Twilio call");
            }

            return result;
        }

        public async Task<FunctionReturnResult<bool>> UpdatePhoneNumberWebhookAsync(string accountSid, string authToken, string phoneNumberId, string webhookUrl)
        {
            var result = new FunctionReturnResult<bool>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Prepare request body
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("VoiceUrl", webhookUrl),
                        new KeyValuePair<string, string>("StatusCallback", $"{webhookUrl}/status")
                    });

                    // Update phone number
                    var response = await client.PostAsync($"Accounts/{accountSid}/IncomingPhoneNumbers/{phoneNumberId}.json", formContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "UpdatePhoneNumberWebhook:1";
                        result.Message = $"Error updating phone number webhook: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    result.Success = true;
                    result.Data = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "UpdatePhoneNumberWebhook:2";
                result.Message = $"Error updating phone number webhook: {ex.Message}";
                _logger.LogError(ex, "Error updating Twilio phone number webhook");
            }

            return result;
        }

        public async Task<FunctionReturnResult<TwilioCallDetails>> GetCallDetailsAsync(string accountSid, string authToken, string callSid)
        {
            var result = new FunctionReturnResult<TwilioCallDetails>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Get call details
                    var response = await client.GetAsync($"Accounts/{accountSid}/Calls/{callSid}.json");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetCallDetails:1";
                        result.Message = $"Error getting call details: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var callDetails = JsonSerializer.Deserialize<TwilioCallDetails>(content, _jsonOptions);

                    if (callDetails == null)
                    {
                        result.Code = "GetCallDetails:2";
                        result.Message = "Failed to deserialize call details";
                        _logger.LogError("Failed to deserialize Twilio call details response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = callDetails;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetCallDetails:3";
                result.Message = $"Error getting call details: {ex.Message}";
                _logger.LogError(ex, "Error getting Twilio call details");
            }

            return result;
        }

        public async Task<FunctionReturnResult<bool>> EndCallAsync(string accountSid, string authToken, string callSid)
        {
            var result = new FunctionReturnResult<bool>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Prepare request body to end the call
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Status", "completed")
                    });

                    // Update call to end it
                    var response = await client.PostAsync($"Accounts/{accountSid}/Calls/{callSid}.json", formContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "EndCall:1";
                        result.Message = $"Error ending call: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    result.Success = true;
                    result.Data = true;
                }
            }
            catch (Exception ex)
            {
                result.Code = "EndCall:2";
                result.Message = $"Error ending call: {ex.Message}";
                _logger.LogError(ex, "Error ending Twilio call");
            }

            return result;
        }

        public async Task<FunctionReturnResult<List<TwilioCallDetails>?>> GetCallsByStatusForPhoneNumber(string accountSid, string authToken, string twilioPhonenumberId, List<string> twilioStatusToCheck, int limit)
        {
            var result = new FunctionReturnResult<List<TwilioCallDetails>?>();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    // Prepare request body
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("To", twilioPhonenumberId),
                        new KeyValuePair<string, string>("Status", string.Join(",", twilioStatusToCheck)),
                        new KeyValuePair<string, string>("limit", limit.ToString())
                    });

                    // Get calls
                    var response = await client.PostAsync("Calls.json", formContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "GetCallsByStatusForPhoneNumber:1";
                        result.Message = $"Error getting calls for phone number: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var calls = JsonSerializer.Deserialize<List<TwilioCallDetails>>(content, _jsonOptions);

                    if (calls == null)
                    {
                        result.Code = "GetCallsByStatusForPhoneNumber:2";
                        result.Message = "Failed to deserialize calls list";
                        _logger.LogError("Failed to deserialize Twilio calls response");
                        return result;
                    }

                    result.Success = true;
                    result.Data = calls;
                }
            }
            catch (Exception ex)
            {
                result.Code = "GetCallsByStatusForPhoneNumber:3";
                result.Message = $"Error getting calls for phone number: {ex.Message}";
                _logger.LogError(ex, "Error getting Twilio calls for phone number");
            }

            return result;
        }

        public async Task<FunctionReturnResult> UpdatePhoneNumberVoiceConfigurationAsync(string accountSid, string authToken, string phoneNumberId, string voiceUrl, string statusCallbackUrl, string voiceMethod = "POST", string statusCallbackMethod = "POST", string voiceReceiveMode = "voice")
        {
            var result = new FunctionReturnResult();

            try
            {
                using (var client = CreateConfiguredHttpClient(accountSid, authToken))
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("VoiceUrl", voiceUrl),
                        new KeyValuePair<string, string>("VoiceMethod", voiceMethod), 
                        new KeyValuePair<string, string>("VoiceFallbackUrl", voiceUrl),
                        new KeyValuePair<string, string>("VoiceFallbackMethod", voiceMethod),
                        new KeyValuePair<string, string>("StatusCallback", statusCallbackUrl),
                        new KeyValuePair<string, string>("StatusCallbackMethod", statusCallbackMethod),
                        new KeyValuePair<string, string>("VoiceReceiveMode", voiceReceiveMode)
                    });

                    var response = await client.PostAsync($"Accounts/{accountSid}/IncomingPhoneNumbers/{phoneNumberId}.json", formContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Code = "UpdatePhoneNumberVoiceConfiguration:1";
                        result.Message = $"Error updating phone number voice configuration: {response.StatusCode}. Details: {errorContent}";
                        _logger.LogError("Twilio API error: {StatusCode}, {Error}", response.StatusCode, errorContent);
                        return result;
                    }

                    return result.SetSuccessResult();
                }
            }
            catch (Exception ex)
            {
                result.Code = "UpdatePhoneNumberVoiceConfiguration:2";
                result.Message = $"Error updating phone number voice configuration: {ex.Message}";
                _logger.LogError(ex, "Error updating Twilio phone number voice configuration");

                return result;
            }
        }
    }
}
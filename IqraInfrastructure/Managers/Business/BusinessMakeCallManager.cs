using IqraCore.Entities.Business;
using IqraCore.Entities.Helper.Server;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Region;
using IqraCore.Models.Business.MakeCalls;
using IqraInfrastructure.Managers.Region;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace IqraInfrastructure.Managers.Business
{
    public class BusinessMakeCallManager
    {
        private readonly ILogger<BusinessMakeCallManager> _logger;
        private readonly BusinessManager _parentBusinessManager;
        private readonly RegionManager _regionManager;
        private readonly IHttpClientFactory _httpClientFactory;

        public BusinessMakeCallManager(
            ILogger<BusinessMakeCallManager> logger,
            BusinessManager parentBusinessManager,
            RegionManager regionManager,
            IHttpClientFactory httpClientFactory
            )
        {
            _logger = logger;
            _parentBusinessManager = parentBusinessManager;
            _regionManager = regionManager;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<FunctionReturnResult<object>> ForwardCallInitiationRequestAsync(long businessId, MakeCallRequestDto callConfig, IFormFile? bulkCsvFile)
        {
            var result = new FunctionReturnResult<object>();

            // --- Input Validation (High Level) ---
            if (callConfig?.NumberDetails?.FromNumberId == null)
            {
                result.Code = "ForwardCall:1"; result.Message = "Missing 'From' number ID in configuration."; return result;
            }
            if (callConfig?.AgentSettings?.AgentId == null)
            {
                result.Code = "ForwardCall:2"; result.Message = "Missing Agent ID in configuration."; return result;
            }
            if (callConfig?.AgentSettings?.LanguageCode == null)
            {
                result.Code = "ForwardCall:2.1"; result.Message = "Missing Language Code in configuration."; return result;
            }


            // --- Region/Proxy Selection ---
            BusinessNumberData? numberData;
            try
            {
                numberData = await _parentBusinessManager.GetNumberManager().GetBusinessNumberById(businessId, callConfig.NumberDetails.FromNumberId);
            }
            catch (Exception ex) // Catch potential manager init issues
            {
                _logger.LogError(ex, "Error getting Number Manager or Number Data for Business {BusinessId}, Number {NumberId}", businessId, callConfig.NumberDetails.FromNumberId);
                result.Code = "ForwardCall:3"; result.Message = "Internal error retrieving number details."; return result;
            }

            if (numberData == null || string.IsNullOrEmpty(numberData.RegionId))
            {
                result.Code = "ForwardCall:4"; result.Message = "'From' number not found or has no associated region."; return result;
            }

            RegionData? regionData = await _regionManager.GetRegionById(numberData.RegionId);
            if (regionData == null)
            {
                result.Code = "ForwardCall:5"; result.Message = $"Region data not found for region ID: {numberData.RegionId}."; return result;
            }

            // Select an *active* Proxy server
            var activeProxy = regionData.Servers.FirstOrDefault(s => s.Type == ServerTypeEnum.Proxy && s.DisabledAt == null);
            if (activeProxy == null)
            {
                result.Code = "ForwardCall:6"; result.Message = $"No active Proxy server found for region: {regionData.CountryRegion}."; return result;
            }
            string proxyEndpoint = activeProxy.Endpoint;
            string proxyApiKey = activeProxy.APIKey; // Assuming APIKey is stored directly

            // Ensure endpoint includes scheme (http/https)
            if (!proxyEndpoint.StartsWith("http://") && !proxyEndpoint.StartsWith("https://"))
            {
                proxyEndpoint = (activeProxy.UseSSL ? "https://" : "http://") + proxyEndpoint;
            }


            // --- Forwarding ---
            string targetUrl;
            HttpContent requestContent;
            bool isBulk = bulkCsvFile != null;

            try
            {
                if (isBulk)
                {
                    targetUrl = $"{proxyEndpoint.TrimEnd('/')}/api/makecall/bulk";
                    var multipartContent = new MultipartFormDataContent();

                    // Add config JSON part
                    string configJson = JsonSerializer.Serialize(callConfig);
                    multipartContent.Add(new StringContent(configJson, Encoding.UTF8, "application/json"), "config");

                    // Add file part
                    var fileStreamContent = new StreamContent(bulkCsvFile!.OpenReadStream());
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv"); // Set content type
                    multipartContent.Add(fileStreamContent, "bulk_file", bulkCsvFile.FileName);

                    requestContent = multipartContent;
                }
                else
                {
                    targetUrl = $"{proxyEndpoint.TrimEnd('/')}/api/makecall/single";
                    string configJson = JsonSerializer.Serialize(callConfig);
                    requestContent = new StringContent(configJson, Encoding.UTF8, "application/json");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing request content for proxy forwarding.");
                result.Code = "ForwardCall:7"; result.Message = "Error preparing request data."; return result;
            }


            try
            {
                using var client = _httpClientFactory.CreateClient("ProxyForwarder"); // Use named client
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-API-Key", proxyApiKey);
                if (!isBulk) client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Forwarding call initiation request to {TargetUrl}", targetUrl);

                HttpResponseMessage response = await client.PostAsync(targetUrl, requestContent);

                // --- Response Handling ---
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Proxy server at {TargetUrl} returned error {StatusCode}. Body: {ErrorBody}", targetUrl, response.StatusCode, errorBody);
                    result.Code = $"ForwardCall:Proxy{(int)response.StatusCode}";
                    // Try to parse error response from proxy if it follows FunctionReturnResult structure
                    try
                    {
                        var proxyErrorResult = JsonSerializer.Deserialize<FunctionReturnResult<object>>(errorBody);
                        result.Message = $"Proxy Error: {proxyErrorResult?.Message ?? response.ReasonPhrase}";
                    }
                    catch
                    {
                        result.Message = $"Proxy returned status {response.StatusCode}: {response.ReasonPhrase}";
                    }

                    // Clean up multipart content if created
                    if (isBulk) requestContent?.Dispose();

                    return result;
                }

                // Success from Proxy
                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Received successful response from proxy: {ResponseBody}", responseBody);
                try
                {
                    // Assume proxy returns FunctionReturnResult<T> structure
                    var proxyResult = JsonSerializer.Deserialize<FunctionReturnResult<object>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    result.Success = proxyResult?.Success ?? true; // Assume success if proxy returned 2xx and valid JSON
                    result.Message = proxyResult?.Message ?? "Request processed by proxy.";
                    result.Data = proxyResult?.Data; // Forward any data returned by the proxy
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogWarning(jsonEx, "Failed to deserialize successful proxy response body: {ResponseBody}", responseBody);
                    result.Success = true; // Still treat as overall success because proxy returned 2xx
                    result.Message = "Request processed by proxy (response format unclear).";
                    result.Data = responseBody; // Return raw response body as data
                }

                // Clean up multipart content if created
                if (isBulk) requestContent?.Dispose();

                return result;

            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error forwarding request to proxy {TargetUrl}", targetUrl);
                result.Code = "ForwardCall:HttpErr"; result.Message = $"Error communicating with proxy server: {httpEx.Message}";
                if (isBulk) requestContent?.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error forwarding request to proxy {TargetUrl}", targetUrl);
                result.Code = "ForwardCall:Ex"; result.Message = $"Internal error forwarding request: {ex.Message}";
                if (isBulk) requestContent?.Dispose();
                return result;
            }
        }
    }
}

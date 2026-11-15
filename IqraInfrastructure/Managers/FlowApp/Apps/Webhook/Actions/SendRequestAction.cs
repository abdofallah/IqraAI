using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Webhook.Actions
{
    public partial class SendRequestAction : IFlowAction
    {
        private readonly WebhookApp _app;

        public SendRequestAction(WebhookApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendRequest";
        public string Name => "Send Request";
        public string Description => "Sends an HTTP request (POST/PUT/PATCH/GET/DELETE) with an optional JSON body to any URL. The universal bridge to Make.com, n8n, Zapier, or your own server.";

        public bool RequiresIntegration => false;

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success (2xx)" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                // 1. Target URL (required)
                if (!input.TryGetProperty("url", out var urlProp) || string.IsNullOrWhiteSpace(urlProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Webhook URL is required.");
                }

                var url = urlProp.GetString()!;
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Webhook URL must be a valid absolute http(s) URL.");
                }

                // 2. HTTP Method
                var methodName = input.TryGetProperty("method", out var methodProp) && !string.IsNullOrWhiteSpace(methodProp.GetString())
                    ? methodProp.GetString()!.Trim().ToUpperInvariant()
                    : "POST";
                var method = new HttpMethod(methodName);

                var httpRequest = new HttpRequestMessage(method, uri);

                // 3. Build the JSON body.
                string bodyJson = "{}";
                bool hasBody = method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch;

                if (input.TryGetProperty("payload", out var payloadProp) && payloadProp.ValueKind != JsonValueKind.Null && payloadProp.ValueKind != JsonValueKind.Undefined)
                {
                    if (payloadProp.ValueKind == JsonValueKind.String)
                    {
                        var raw = payloadProp.GetString() ?? string.Empty;
                        bodyJson = string.IsNullOrWhiteSpace(raw) ? "{}" : raw;
                    }
                    else
                    {
                        bodyJson = payloadProp.GetRawText();
                    }
                }

                if (hasBody)
                {
                    httpRequest.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                }

                // 4. Optional custom headers
                if (input.TryGetProperty("headers", out var headersProp) && headersProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var header in headersProp.EnumerateObject())
                    {
                        var value = header.Value.ValueKind == JsonValueKind.String ? header.Value.GetString() : header.Value.GetRawText();
                        if (string.IsNullOrEmpty(value)) continue;

                        if (httpRequest.Content != null && header.Name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        {
                            httpRequest.Content.Headers.TryAddWithoutValidation(header.Name, value);
                        }
                        else
                        {
                            httpRequest.Headers.TryAddWithoutValidation(header.Name, value);
                        }
                    }
                }

                // 5. Optional auth.
                if (input.TryGetProperty("bearerToken", out var bearerProp) && !string.IsNullOrWhiteSpace(bearerProp.GetString()))
                {
                    httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {bearerProp.GetString()}");
                }

                if (input.TryGetProperty("hmacSecret", out var secretProp) && !string.IsNullOrWhiteSpace(secretProp.GetString()))
                {
                    var headerName = input.TryGetProperty("hmacHeader", out var hmacHeaderProp) && !string.IsNullOrWhiteSpace(hmacHeaderProp.GetString())
                        ? hmacHeaderProp.GetString()!
                        : "X-Signature";

                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretProp.GetString()!));
                    var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(hasBody ? bodyJson : string.Empty))).ToLowerInvariant();
                    httpRequest.Headers.TryAddWithoutValidation(headerName, signature);
                }

                // 6. Execute.
                var client = _app.CreateClient();
                var response = await client.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("HTTP_ERROR", $"Webhook returned {(int)response.StatusCode} {response.StatusCode}: {Truncate(responseBody)}");
                }

                // 7. Result
                object? parsedBody = TryParseJson(responseBody);

                return ActionExecutionResult.SuccessPort("success", new
                {
                    statusCode = (int)response.StatusCode,
                    body = parsedBody
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }

        private static object? TryParseJson(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            try
            {
                using var doc = JsonDocument.Parse(content);
                return doc.RootElement.Clone();
            }
            catch
            {
                return content;
            }
        }

        private static string Truncate(string value, int max = 500)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return value.Substring(0, max) + "…";
        }
    }
}

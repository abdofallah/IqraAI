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
        private const int MaxRedirects = 20;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(100);

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

                var (isAllowed, securityReason) = await _app.IsEndpointAllowedAsync(uri.AbsoluteUri);
                if (!isAllowed)
                {
                    return ActionExecutionResult.Failure("ENDPOINT_NOT_ALLOWED", $"Webhook URL failed the endpoint security check: {securityReason}");
                }

                // 2. HTTP Method
                var methodName = input.TryGetProperty("method", out var methodProp) && !string.IsNullOrWhiteSpace(methodProp.GetString())
                    ? methodProp.GetString()!.Trim().ToUpperInvariant()
                    : "POST";
                var method = new HttpMethod(methodName);

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

                // 4. Optional custom headers
                var headers = new List<KeyValuePair<string, string>>();
                if (input.TryGetProperty("headers", out var headersProp) && headersProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var header in headersProp.EnumerateObject())
                    {
                        var value = header.Value.ValueKind == JsonValueKind.String ? header.Value.GetString() : header.Value.GetRawText();
                        if (string.IsNullOrEmpty(value)) continue;

                        headers.Add(new KeyValuePair<string, string>(header.Name, value));
                    }
                }

                // 5. Optional auth.
                if (input.TryGetProperty("bearerToken", out var bearerProp) && !string.IsNullOrWhiteSpace(bearerProp.GetString()))
                {
                    headers.Add(new KeyValuePair<string, string>("Authorization", $"Bearer {bearerProp.GetString()}"));
                }

                string? signatureHeaderName = null;
                if (input.TryGetProperty("hmacSecret", out var secretProp) && !string.IsNullOrWhiteSpace(secretProp.GetString()))
                {
                    signatureHeaderName = input.TryGetProperty("hmacHeader", out var hmacHeaderProp) && !string.IsNullOrWhiteSpace(hmacHeaderProp.GetString())
                        ? hmacHeaderProp.GetString()!
                        : "X-Signature";

                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretProp.GetString()!));
                    var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(hasBody ? bodyJson : string.Empty))).ToLowerInvariant();
                    headers.Add(new KeyValuePair<string, string>(signatureHeaderName, signature));
                }

                // 6. Execute.
                var client = _app.CreateClient();
                using var timeout = new CancellationTokenSource(RequestTimeout);

                var currentUri = uri;
                var currentMethod = method;
                var sendBody = hasBody;
                var sendCredentials = true;
                var redirects = 0;
                HttpResponseMessage response;

                while (true)
                {
                    using var httpRequest = BuildRequest(currentMethod, currentUri, sendBody ? bodyJson : null, headers, signatureHeaderName, sendCredentials);
                    response = await client.SendAsync(httpRequest, timeout.Token);

                    if (!Equals(response.RequestMessage?.RequestUri, currentUri))
                    {
                        response.Dispose();
                        return ActionExecutionResult.Failure("ENDPOINT_NOT_ALLOWED", "The webhook request was redirected without the endpoint security check, so its reply is not used.");
                    }

                    if (!IsRedirect(response.StatusCode) || response.Headers.Location == null)
                    {
                        break;
                    }

                    var nextUri = new Uri(currentUri, response.Headers.Location);
                    var statusCode = response.StatusCode;
                    response.Dispose();

                    if (redirects == MaxRedirects)
                    {
                        return ActionExecutionResult.Failure("TOO_MANY_REDIRECTS", $"Webhook URL redirected more than {MaxRedirects} times.");
                    }

                    if (nextUri.Scheme != Uri.UriSchemeHttp && nextUri.Scheme != Uri.UriSchemeHttps)
                    {
                        return ActionExecutionResult.Failure("REDIRECT_NOT_FOLLOWED", "Webhook URL redirected to an address that is not http(s).");
                    }

                    if (currentUri.Scheme == Uri.UriSchemeHttps && nextUri.Scheme == Uri.UriSchemeHttp)
                    {
                        return ActionExecutionResult.Failure("REDIRECT_NOT_FOLLOWED", "Webhook URL redirected from https to http, which is not followed.");
                    }

                    var (redirectAllowed, redirectReason) = await _app.IsEndpointAllowedAsync(nextUri.AbsoluteUri);
                    if (!redirectAllowed)
                    {
                        return ActionExecutionResult.Failure("ENDPOINT_NOT_ALLOWED", $"Webhook redirect target failed the endpoint security check: {redirectReason}");
                    }

                    if (!string.Equals(nextUri.Host, currentUri.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        sendCredentials = false;
                    }

                    if ((statusCode == HttpStatusCode.SeeOther && currentMethod != HttpMethod.Head) ||
                        ((statusCode == HttpStatusCode.MovedPermanently || statusCode == HttpStatusCode.Found) && currentMethod == HttpMethod.Post))
                    {
                        currentMethod = HttpMethod.Get;
                        sendBody = false;
                    }

                    currentUri = nextUri;
                    redirects++;
                }

                using (response)
                {
                    var responseBody = await response.Content.ReadAsStringAsync(timeout.Token);

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
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }

        private static HttpRequestMessage BuildRequest(HttpMethod method, Uri uri, string? bodyJson, List<KeyValuePair<string, string>> headers, string? signatureHeaderName, bool sendCredentials)
        {
            var httpRequest = new HttpRequestMessage(method, uri);
            if (bodyJson != null)
            {
                httpRequest.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            }

            foreach (var header in headers)
            {
                if (!sendCredentials && (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || header.Key.Equals(signatureHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (httpRequest.Content != null && header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    httpRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                else
                {
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return httpRequest;
        }

        private static bool IsRedirect(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.MovedPermanently ||
                statusCode == HttpStatusCode.Found ||
                statusCode == HttpStatusCode.SeeOther ||
                statusCode == HttpStatusCode.TemporaryRedirect ||
                statusCode == HttpStatusCode.PermanentRedirect;
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

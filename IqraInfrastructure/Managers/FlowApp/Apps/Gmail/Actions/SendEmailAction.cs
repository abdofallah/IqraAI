using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Gmail.Actions
{
    public partial class SendEmailAction : IFlowAction
    {
        private readonly GmailApp _app;

        public SendEmailAction(GmailApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendEmail";
        public string Name => "Send Email";
        public string Description => "Sends an email from the connected Gmail account.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                var accessToken = integration!.OAuthAccessToken!;
                var client = _app.CreateClient(accessToken);

                var to = input.GetProperty("to").GetString();
                var subject = input.GetProperty("subject").GetString() ?? string.Empty;
                var body = input.GetProperty("body").GetString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(to))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Recipient (to) is required.");
                }

                var isHtml = input.TryGetProperty("isHtml", out var h) && h.ValueKind == JsonValueKind.True;
                var contentType = isHtml ? "text/html" : "text/plain";

                var sb = new StringBuilder();
                sb.Append($"To: {to}\r\n");
                if (input.TryGetProperty("cc", out var cc) && !string.IsNullOrWhiteSpace(cc.GetString()))
                {
                    sb.Append($"Cc: {cc.GetString()}\r\n");
                }
                sb.Append($"Subject: {subject}\r\n");
                sb.Append("MIME-Version: 1.0\r\n");
                sb.Append($"Content-Type: {contentType}; charset=UTF-8\r\n");
                sb.Append("\r\n");
                sb.Append(body);

                var raw = Base64UrlEncode(Encoding.UTF8.GetBytes(sb.ToString()));
                var payload = JsonSerializer.Serialize(new { raw });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/gmail/v1/users/me/messages/send")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Gmail error: {response.StatusCode} - {responseContent}");
                }

                using var doc = JsonDocument.Parse(responseContent);
                var messageId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var threadId = doc.RootElement.TryGetProperty("threadId", out var thEl) ? thEl.GetString() : null;

                return ActionExecutionResult.SuccessPort("success", new { messageId, threadId });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}

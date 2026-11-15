using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.SendGrid.Actions
{
    public partial class SendEmailAction : IFlowAction
    {
        private readonly SendGridApp _app;

        public SendEmailAction(SendGridApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendEmail";
        public string Name => "Send Email";
        public string Description => "Sends a transactional email via SendGrid (e.g. a call summary or confirmation to the customer).";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Sent" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                var apiKey = integration!.DecryptedFields["ApiKey"];
                var client = _app.CreateClient(apiKey);

                var to = input.GetProperty("to").GetString();
                var subject = input.GetProperty("subject").GetString();
                var content = input.GetProperty("content").GetString();
                var fromEmail = input.GetProperty("fromEmail").GetString();

                if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(subject) ||
                    string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(fromEmail))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Recipient, sender, subject and content are required.");
                }

                var isHtml = input.TryGetProperty("isHtml", out var htmlProp) && htmlProp.ValueKind == JsonValueKind.True;
                var contentType = isHtml ? "text/html" : "text/plain";

                var fromName = input.TryGetProperty("fromName", out var fn) ? fn.GetString() : null;

                var body = new
                {
                    personalizations = new[]
                    {
                        new { to = new[] { new { email = to } }, subject }
                    },
                    from = new { email = fromEmail, name = fromName },
                    content = new[]
                    {
                        new { type = contentType, value = content }
                    }
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v3/mail/send")
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);

                if (response.StatusCode == HttpStatusCode.Accepted || response.IsSuccessStatusCode)
                {
                    var messageId = response.Headers.TryGetValues("X-Message-Id", out var ids) ? ids.FirstOrDefault() : null;
                    return ActionExecutionResult.SuccessPort("success", new { statusCode = (int)response.StatusCode, messageId });
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return ActionExecutionResult.Failure("API_ERROR", $"SendGrid Error: {response.StatusCode} - {errorContent}");
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

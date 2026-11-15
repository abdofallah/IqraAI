using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.OutlookMail.Actions
{
    public partial class SendEmailAction : IFlowAction
    {
        private readonly OutlookMailApp _app;

        public SendEmailAction(OutlookMailApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendEmail";
        public string Name => "Send Email";
        public string Description => "Sends an email from the connected Outlook / Microsoft 365 account.";

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
                if (string.IsNullOrWhiteSpace(to))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Recipient (to) is required.");
                }

                var isHtml = input.TryGetProperty("isHtml", out var h) && h.ValueKind == JsonValueKind.True;

                var toRecipients = to
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(addr => new Dictionary<string, object?>
                    {
                        ["emailAddress"] = new Dictionary<string, object?> { ["address"] = addr.Trim() }
                    })
                    .ToList();

                var message = new Dictionary<string, object?>
                {
                    ["subject"] = input.GetProperty("subject").GetString() ?? string.Empty,
                    ["body"] = new Dictionary<string, object?>
                    {
                        ["contentType"] = isHtml ? "HTML" : "Text",
                        ["content"] = input.GetProperty("body").GetString() ?? string.Empty
                    },
                    ["toRecipients"] = toRecipients
                };

                var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["message"] = message,
                    ["saveToSentItems"] = true
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1.0/me/sendMail")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return ProviderReplyHelper.ActionFailure("Microsoft Graph error", response.StatusCode, content);
                }

                return ActionExecutionResult.SuccessPort("success", new { sent = true });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

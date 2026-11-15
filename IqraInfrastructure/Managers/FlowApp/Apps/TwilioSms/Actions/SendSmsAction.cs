using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.TwilioSms.Actions
{
    public partial class SendSmsAction : IFlowAction
    {
        private readonly TwilioSmsApp _app;

        public SendSmsAction(TwilioSmsApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendSms";
        public string Name => "Send SMS";
        public string Description => "Sends a text message via Twilio (e.g. send the caller a booking confirmation or link).";

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
                var accountSid = integration!.DecryptedFields["AccountSid"];
                var authToken = integration.DecryptedFields["AuthToken"];
                var client = _app.CreateClient(accountSid, authToken);

                var to = input.GetProperty("to").GetString();
                var body = input.GetProperty("body").GetString();

                if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(body))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Recipient ('to') and message ('body') are required.");
                }

                var form = new Dictionary<string, string>
                {
                    ["To"] = to,
                    ["Body"] = body
                };

                if (input.TryGetProperty("messagingServiceSid", out var svc) && !string.IsNullOrWhiteSpace(svc.GetString()))
                {
                    form["MessagingServiceSid"] = svc.GetString()!;
                }
                else if (input.TryGetProperty("from", out var from) && !string.IsNullOrWhiteSpace(from.GetString()))
                {
                    form["From"] = from.GetString()!;
                }
                else
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Provide either a 'From' number or a Messaging Service SID.");
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/2010-04-01/Accounts/{Uri.EscapeDataString(accountSid)}/Messages.json")
                {
                    Content = new FormUrlEncodedContent(form)
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Twilio Error: {response.StatusCode} - {content}");
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                return ActionExecutionResult.SuccessPort("success", new
                {
                    sid = root.TryGetProperty("sid", out var sid) ? sid.GetString() : null,
                    status = root.TryGetProperty("status", out var status) ? status.GetString() : null,
                    to = root.TryGetProperty("to", out var toEl) ? toEl.GetString() : null
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

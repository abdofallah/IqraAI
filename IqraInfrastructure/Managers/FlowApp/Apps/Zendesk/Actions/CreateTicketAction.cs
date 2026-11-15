using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Zendesk.Actions
{
    public partial class CreateTicketAction : IFlowAction
    {
        private readonly ZendeskApp _app;

        public CreateTicketAction(ZendeskApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateTicket";
        public string Name => "Create Ticket";
        public string Description => "Opens a Zendesk support ticket, e.g. log an issue the caller reported and route it to the right team.";

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
                var subdomain = integration!.DecryptedFields["Subdomain"];
                var email = integration.DecryptedFields["Email"];
                var apiToken = integration.DecryptedFields["ApiToken"];
                var client = _app.CreateClient(subdomain, email, apiToken);

                var subject = input.GetProperty("subject").GetString();
                var comment = input.GetProperty("comment").GetString();

                if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(comment))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Subject and comment are required.");
                }

                var ticket = new Dictionary<string, object?>
                {
                    ["subject"] = subject,
                    ["comment"] = new { body = comment }
                };

                if (input.TryGetProperty("priority", out var priority) && !string.IsNullOrWhiteSpace(priority.GetString()))
                    ticket["priority"] = priority.GetString();

                var requesterName = input.TryGetProperty("requesterName", out var rn) ? rn.GetString() : null;
                var requesterEmail = input.TryGetProperty("requesterEmail", out var re) ? re.GetString() : null;
                if (!string.IsNullOrWhiteSpace(requesterName) || !string.IsNullOrWhiteSpace(requesterEmail))
                {
                    ticket["requester"] = new { name = requesterName, email = requesterEmail };
                }

                if (input.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                {
                    ticket["tags"] = tagsProp.EnumerateArray().Select(t => t.GetString()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                }

                var body = new { ticket };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/tickets.json")
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Zendesk Error", response.StatusCode, content);
                }

                using var doc = JsonDocument.Parse(content);
                var ticketEl = doc.RootElement.GetProperty("ticket");

                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = ticketEl.TryGetProperty("id", out var id) ? id.GetInt64() : (long?)null,
                    status = ticketEl.TryGetProperty("status", out var status) ? status.GetString() : null,
                    url = ticketEl.TryGetProperty("url", out var url) ? url.GetString() : null
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

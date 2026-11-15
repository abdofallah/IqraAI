using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Salesforce.Actions
{
    public partial class CreateLeadAction : IFlowAction
    {
        private readonly SalesforceApp _app;

        public CreateLeadAction(SalesforceApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateLead";
        public string Name => "Create Lead";
        public string Description => "Creates a Salesforce Lead. Ideal for capturing a prospect from an inbound call.";

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
                integration.OAuthExtraFields.TryGetValue("instance_url", out var instanceUrl);

                var client = _app.CreateClient(accessToken, instanceUrl);
                if (client == null)
                {
                    return ActionExecutionResult.Failure("NOT_CONNECTED", "Salesforce instance URL is missing or is not a Salesforce address. Reconnect the integration.");
                }

                var lastName = input.GetProperty("lastName").GetString();
                if (string.IsNullOrWhiteSpace(lastName))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Last name is required for a Salesforce Lead.");
                }

                var fields = new Dictionary<string, object?> { ["LastName"] = lastName };
                AddIfPresent(input, "firstName", "FirstName", fields);
                AddIfPresent(input, "company", "Company", fields);
                AddIfPresent(input, "email", "Email", fields);
                AddIfPresent(input, "phone", "Phone", fields);
                AddIfPresent(input, "title", "Title", fields);
                AddIfPresent(input, "status", "Status", fields);

                if (!fields.ContainsKey("Company"))
                {
                    fields["Company"] = "[Not provided]";
                }

                var payload = JsonSerializer.Serialize(fields);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"services/data/{SalesforceApp.ApiVersion}/sobjects/Lead")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Salesforce error", response.StatusCode, content);
                }

                using var doc = JsonDocument.Parse(content);
                var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                return ActionExecutionResult.SuccessPort("success", new { id });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }

        private static void AddIfPresent(JsonElement input, string inputKey, string sfField, Dictionary<string, object?> fields)
        {
            if (input.TryGetProperty(inputKey, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()))
            {
                fields[sfField] = v.GetString();
            }
        }
    }
}

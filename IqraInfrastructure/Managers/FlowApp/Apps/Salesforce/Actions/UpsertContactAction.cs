using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Salesforce.Actions
{
    public partial class UpsertContactAction : IFlowAction
    {
        private readonly SalesforceApp _app;

        public UpsertContactAction(SalesforceApp app)
        {
            _app = app;
        }

        public string ActionKey => "UpsertContact";
        public string Name => "Create or Update Contact";
        public string Description => "Creates a Salesforce Contact, or updates the existing one matching the email.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "created", Label = "Created" },
                new ActionOutputPort { Key = "updated", Label = "Updated" },
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
                    return ActionExecutionResult.Failure("NOT_CONNECTED", "Salesforce instance URL is missing. Reconnect the integration.");
                }

                var email = input.GetProperty("email").GetString();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Email is required to upsert a Contact.");
                }

                var lastName = input.TryGetProperty("lastName", out var ln) ? ln.GetString() : null;

                var fields = new Dictionary<string, object?> { ["Email"] = email };
                if (!string.IsNullOrWhiteSpace(lastName)) fields["LastName"] = lastName;
                AddIfPresent(input, "firstName", "FirstName", fields);
                AddIfPresent(input, "phone", "Phone", fields);
                AddIfPresent(input, "title", "Title", fields);

                // 1. Find an existing contact by email (SOQL).
                var soql = $"SELECT Id FROM Contact WHERE Email = '{email.Replace("'", "\\'")}' LIMIT 1";
                var queryUrl = $"services/data/{SalesforceApp.ApiVersion}/query?q={HttpUtility.UrlEncode(soql)}";
                var queryResponse = await client.GetAsync(queryUrl);
                var queryContent = await queryResponse.Content.ReadAsStringAsync();

                if (!queryResponse.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Salesforce query error: {queryResponse.StatusCode} - {queryContent}");
                }

                using var queryDoc = JsonDocument.Parse(queryContent);
                string? existingId = null;
                if (queryDoc.RootElement.TryGetProperty("records", out var records) &&
                    records.ValueKind == JsonValueKind.Array && records.GetArrayLength() > 0)
                {
                    existingId = records[0].TryGetProperty("Id", out var idEl) ? idEl.GetString() : null;
                }

                if (!string.IsNullOrEmpty(existingId))
                {
                    // 2a. Update the existing contact.
                    var patchBody = new Dictionary<string, object?>(fields);
                    patchBody.Remove("Email");
                    var patchPayload = JsonSerializer.Serialize(patchBody);
                    var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"services/data/{SalesforceApp.ApiVersion}/sobjects/Contact/{existingId}")
                    {
                        Content = new StringContent(patchPayload, Encoding.UTF8, "application/json")
                    };
                    var patchResponse = await client.SendAsync(patchRequest);
                    if (!patchResponse.IsSuccessStatusCode)
                    {
                        var patchContent = await patchResponse.Content.ReadAsStringAsync();
                        return ActionExecutionResult.Failure("API_ERROR", $"Salesforce update error: {patchResponse.StatusCode} - {patchContent}");
                    }
                    return ActionExecutionResult.SuccessPort("updated", new { id = existingId });
                }

                // 2b. Create a new contact.
                if (!fields.ContainsKey("LastName"))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Last name is required to create a new Contact.");
                }

                var createPayload = JsonSerializer.Serialize(fields);
                var createRequest = new HttpRequestMessage(HttpMethod.Post, $"services/data/{SalesforceApp.ApiVersion}/sobjects/Contact")
                {
                    Content = new StringContent(createPayload, Encoding.UTF8, "application/json")
                };
                var createResponse = await client.SendAsync(createRequest);
                var createContent = await createResponse.Content.ReadAsStringAsync();

                if (!createResponse.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Salesforce create error: {createResponse.StatusCode} - {createContent}");
                }

                using var createDoc = JsonDocument.Parse(createContent);
                var newId = createDoc.RootElement.TryGetProperty("id", out var newIdEl) ? newIdEl.GetString() : null;
                return ActionExecutionResult.SuccessPort("created", new { id = newId });
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

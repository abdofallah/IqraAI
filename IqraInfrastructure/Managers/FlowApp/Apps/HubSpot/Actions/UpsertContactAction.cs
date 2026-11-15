using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Models;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Actions
{
    public partial class UpsertContactAction : IFlowAction
    {
        private readonly HubSpotApp _app;

        public UpsertContactAction(HubSpotApp app)
        {
            _app = app;
        }

        public string ActionKey => "UpsertContact";
        public string Name => "Create or Update Contact";
        public string Description => "Creates a HubSpot contact, or updates the existing one matching the email. Ideal for logging a lead captured on a call.";

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
                var token = integration!.DecryptedFields["AccessToken"];
                var client = _app.CreateClient(token);

                var email = input.GetProperty("email").GetString();
                if (string.IsNullOrWhiteSpace(email))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Contact email is required.");
                }

                var properties = new Dictionary<string, object?> { ["email"] = email };
                if (input.TryGetProperty("properties", out var extra) && extra.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in extra.EnumerateObject())
                    {
                        if (prop.NameEquals("email")) continue;
                        properties[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
                    }
                }

                // 1. Search for an existing contact by email.
                var searchRequest = new SearchContactsRequest
                {
                    FilterGroups = new List<FilterGroup>
                    {
                        new FilterGroup { Filters = new List<Filter> { new Filter { PropertyName = "email", Operator = "EQ", Value = email } } }
                    },
                    Properties = new List<string> { "email" },
                    Limit = 1
                };

                var searchHttp = new HttpRequestMessage(HttpMethod.Post, "/crm/v3/objects/contacts/search")
                {
                    Content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json")
                };
                var searchResponse = await client.SendAsync(searchHttp);
                var searchContent = await searchResponse.Content.ReadAsStringAsync();

                if (!searchResponse.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"HubSpot search error: {searchResponse.StatusCode} - {searchContent}");
                }

                var searchResult = JsonSerializer.Deserialize<SearchContactsResponse>(searchContent);
                var writeBody = new ContactWriteRequest { Properties = properties };

                if (searchResult != null && searchResult.Total > 0 && searchResult.Results.Count > 0)
                {
                    // 2a. Update the existing contact.
                    var existingId = searchResult.Results[0].Id;
                    var updateHttp = new HttpRequestMessage(HttpMethod.Patch, $"/crm/v3/objects/contacts/{existingId}")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(writeBody), Encoding.UTF8, "application/json")
                    };
                    var updateResponse = await client.SendAsync(updateHttp);
                    var updateContent = await updateResponse.Content.ReadAsStringAsync();

                    if (!updateResponse.IsSuccessStatusCode)
                    {
                        return ActionExecutionResult.Failure("API_ERROR", $"HubSpot update error: {updateResponse.StatusCode} - {updateContent}");
                    }

                    var updated = JsonSerializer.Deserialize<HubSpotContact>(updateContent);
                    return ActionExecutionResult.SuccessPort("updated", new { id = updated?.Id, properties = updated?.Properties });
                }

                // 2b. Create a new contact.
                var createHttp = new HttpRequestMessage(HttpMethod.Post, "/crm/v3/objects/contacts")
                {
                    Content = new StringContent(JsonSerializer.Serialize(writeBody), Encoding.UTF8, "application/json")
                };
                var createResponse = await client.SendAsync(createHttp);
                var createContent = await createResponse.Content.ReadAsStringAsync();

                if (!createResponse.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"HubSpot create error: {createResponse.StatusCode} - {createContent}");
                }

                var created = JsonSerializer.Deserialize<HubSpotContact>(createContent);
                return ActionExecutionResult.SuccessPort("created", new { id = created?.Id, properties = created?.Properties });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

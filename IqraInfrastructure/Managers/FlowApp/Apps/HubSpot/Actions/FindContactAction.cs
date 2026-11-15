using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Actions
{
    public partial class FindContactAction : IFlowAction
    {
        private readonly HubSpotApp _app;

        public FindContactAction(HubSpotApp app)
        {
            _app = app;
        }

        public string ActionKey => "FindContact";
        public string Name => "Find Contact";
        public string Description => "Looks up a HubSpot contact by email so the agent can personalise the conversation.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "found", Label = "Found" },
                new ActionOutputPort { Key = "not_found", Label = "Not Found" },
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

                var searchRequest = new SearchContactsRequest
                {
                    FilterGroups = new List<FilterGroup>
                    {
                        new FilterGroup { Filters = new List<Filter> { new Filter { PropertyName = "email", Operator = "EQ", Value = email } } }
                    },
                    Properties = new List<string> { "email", "firstname", "lastname", "phone", "company", "lifecyclestage" },
                    Limit = 1
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/crm/v3/objects/contacts/search")
                {
                    Content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("HubSpot Error", response.StatusCode, content);
                }

                var result = JsonSerializer.Deserialize<SearchContactsResponse>(content);

                if (result == null || result.Total == 0 || result.Results.Count == 0)
                {
                    return ActionExecutionResult.SuccessPort("not_found", new { email });
                }

                var contact = result.Results[0];
                return ActionExecutionResult.SuccessPort("found", new
                {
                    id = contact.Id,
                    properties = contact.Properties
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

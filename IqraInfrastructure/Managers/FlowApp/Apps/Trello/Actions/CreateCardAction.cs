using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Trello.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Trello.Actions
{
    public partial class CreateCardAction : IFlowAction
    {
        private readonly TrelloApp _app;

        public CreateCardAction(TrelloApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateCard";
        public string Name => "Create Card";
        public string Description => "Adds a card to a Trello list, e.g. create a follow-up task from a call.";

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
                var apiKey = integration!.DecryptedFields["ApiKey"];
                var token = integration.DecryptedFields["Token"];
                var client = _app.CreateClient();

                var listId = input.GetProperty("listId").GetString();
                var name = input.GetProperty("name").GetString();

                if (string.IsNullOrWhiteSpace(listId) || string.IsNullOrWhiteSpace(name))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "List and card name are required.");
                }

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["idList"] = listId;
                query["name"] = name;

                if (input.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
                    query["desc"] = desc.GetString();
                if (input.TryGetProperty("dueDate", out var due) && !string.IsNullOrWhiteSpace(due.GetString()))
                    query["due"] = due.GetString();

                var path = $"/1/cards?{query}&{TrelloApp.BuildAuthQuery(apiKey, token)}";
                var response = await client.PostAsync(path, null);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Trello Error", response.StatusCode, content);
                }

                var card = JsonSerializer.Deserialize<TrelloCard>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = card?.Id,
                    url = card?.ShortUrl ?? card?.Url
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

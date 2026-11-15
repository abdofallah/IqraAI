using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Actions
{
    public partial class CreateActivityAction : IFlowAction
    {
        private readonly PipedriveApp _app;

        public CreateActivityAction(PipedriveApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateActivity";
        public string Name => "Create Activity";
        public string Description => "Schedules an activity (call, task, follow-up) in Pipedrive, e.g. a callback the agent promised the customer.";

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
                var apiToken = integration!.DecryptedFields["ApiToken"];
                var client = _app.CreateClient();

                var subject = input.GetProperty("subject").GetString();
                if (string.IsNullOrWhiteSpace(subject))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Activity subject is required.");
                }

                var body = new Dictionary<string, object?>
                {
                    ["subject"] = subject,
                    ["type"] = input.TryGetProperty("type", out var typeProp) && !string.IsNullOrWhiteSpace(typeProp.GetString())
                        ? typeProp.GetString()
                        : "call"
                };

                if (input.TryGetProperty("dueDate", out var dueDate) && !string.IsNullOrWhiteSpace(dueDate.GetString()))
                    body["due_date"] = dueDate.GetString();
                if (input.TryGetProperty("dueTime", out var dueTime) && !string.IsNullOrWhiteSpace(dueTime.GetString()))
                    body["due_time"] = dueTime.GetString();
                if (input.TryGetProperty("note", out var note) && !string.IsNullOrWhiteSpace(note.GetString()))
                    body["note"] = note.GetString();
                if (input.TryGetProperty("personId", out var personId) && personId.ValueKind == JsonValueKind.Number)
                    body["person_id"] = personId.GetInt32();

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, PipedriveApp.BuildPath("/v1/activities", apiToken))
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Pipedrive Error", response.StatusCode, content);
                }

                using var doc = JsonDocument.Parse(content);
                var data = doc.RootElement.TryGetProperty("data", out var d) ? d : default;

                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("id", out var id) ? id.GetInt32() : (int?)null,
                    subject
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

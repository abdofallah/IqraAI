using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Actions
{
    public partial class CreateRecordAction : IFlowAction
    {
        private readonly AirtableApp _app;

        public CreateRecordAction(AirtableApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateRecord";
        public string Name => "Create Record";
        public string Description => "Creates a new record in an Airtable table (e.g. log a lead captured during a call).";

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
                var accessToken = integration!.DecryptedFields["AccessToken"];
                var client = _app.CreateClient(accessToken);

                var baseId = input.GetProperty("baseId").GetString();
                var tableId = input.GetProperty("tableId").GetString();

                if (string.IsNullOrWhiteSpace(baseId) || string.IsNullOrWhiteSpace(tableId))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Base and Table are required.");
                }

                var request = new CreateRecordRequest();
                if (input.TryGetProperty("fields", out var fieldsProp) && fieldsProp.ValueKind == JsonValueKind.Object)
                {
                    request.Fields = JsonSerializer.Deserialize<Dictionary<string, object?>>(fieldsProp.GetRawText()) ?? new();
                }

                if (request.Fields.Count == 0)
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "At least one field value is required.");
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/v0/{baseId}/{Uri.EscapeDataString(tableId)}")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Airtable Error", response.StatusCode, content);
                }

                var result = JsonSerializer.Deserialize<AirtableRecord>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = result?.Id,
                    createdTime = result?.CreatedTime,
                    fields = result?.Fields
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Models;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Actions
{
    public partial class FindRecordsAction : IFlowAction
    {
        private readonly AirtableApp _app;

        public FindRecordsAction(AirtableApp app)
        {
            _app = app;
        }

        public string ActionKey => "FindRecords";
        public string Name => "Find Records";
        public string Description => "Lists records from a table, optionally filtered with an Airtable formula (e.g. look up a customer by phone number).";

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

                var query = HttpUtility.ParseQueryString(string.Empty);

                if (input.TryGetProperty("filterByFormula", out var formulaProp) && !string.IsNullOrWhiteSpace(formulaProp.GetString()))
                {
                    query["filterByFormula"] = formulaProp.GetString();
                }

                int maxRecords = 10;
                if (input.TryGetProperty("maxRecords", out var maxProp) && maxProp.ValueKind == JsonValueKind.Number)
                {
                    maxRecords = Math.Clamp(maxProp.GetInt32(), 1, 100);
                }
                query["maxRecords"] = maxRecords.ToString();
                query["pageSize"] = maxRecords.ToString();

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v0/{baseId}/{Uri.EscapeDataString(tableId)}?{query}");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Airtable Error: {response.StatusCode} - {content}");
                }

                var result = JsonSerializer.Deserialize<ListRecordsResponse>(content);
                var records = result?.Records ?? new List<AirtableRecord>();

                return ActionExecutionResult.SuccessPort("success", new
                {
                    count = records.Count,
                    records = records.Select(r => new { id = r.Id, fields = r.Fields }).ToList()
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

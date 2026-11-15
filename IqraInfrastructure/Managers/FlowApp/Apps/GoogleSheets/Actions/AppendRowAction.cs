using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleSheets.Actions
{
    public partial class AppendRowAction : IFlowAction
    {
        private readonly GoogleSheetsApp _app;

        public AppendRowAction(GoogleSheetsApp app)
        {
            _app = app;
        }

        public string ActionKey => "AppendRow";
        public string Name => "Append Row";
        public string Description => "Appends a row of values to a sheet. The 'values' field accepts a comma-separated list of cell values.";

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
                var client = _app.CreateClient(accessToken);

                var spreadsheetId = input.GetProperty("spreadsheetId").GetString();
                if (string.IsNullOrWhiteSpace(spreadsheetId))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Spreadsheet is required.");
                }

                var range = input.TryGetProperty("range", out var r) && !string.IsNullOrWhiteSpace(r.GetString())
                    ? r.GetString()!
                    : "Sheet1";

                var rowValues = new List<object?>();
                if (input.TryGetProperty("values", out var valuesEl))
                {
                    if (valuesEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in valuesEl.EnumerateArray())
                        {
                            rowValues.Add(item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText());
                        }
                    }
                    else if (valuesEl.ValueKind == JsonValueKind.String)
                    {
                        var s = valuesEl.GetString() ?? string.Empty;
                        rowValues.AddRange(s.Split(',').Select(v => (object?)v.Trim()));
                    }
                }

                if (rowValues.Count == 0)
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "At least one value is required.");
                }

                var payload = JsonSerializer.Serialize(new { values = new[] { rowValues } });

                var url = $"/sheets/v4/spreadsheets/{Uri.EscapeDataString(spreadsheetId)}/values/{Uri.EscapeDataString(range)}:append?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Google Sheets error: {response.StatusCode} - {content}");
                }

                using var doc = JsonDocument.Parse(content);
                string? updatedRange = null;
                if (doc.RootElement.TryGetProperty("updates", out var updates) && updates.TryGetProperty("updatedRange", out var ur))
                {
                    updatedRange = ur.GetString();
                }

                return ActionExecutionResult.SuccessPort("success", new { updatedRange });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Notion.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Notion.Actions
{
    public partial class CreateDatabasePageAction : IFlowAction
    {
        private readonly NotionApp _app;

        public CreateDatabasePageAction(NotionApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateDatabasePage";
        public string Name => "Create Database Item";
        public string Description => "Adds a new page (row) to a Notion database, e.g. log a call summary or a new lead.";

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
                var token = integration!.DecryptedFields["Token"];
                var client = _app.CreateClient(token);

                var databaseId = input.GetProperty("databaseId").GetString();
                if (string.IsNullOrWhiteSpace(databaseId))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Database is required.");
                }

                var titlePropertyName = input.TryGetProperty("titlePropertyName", out var tpn) && !string.IsNullOrWhiteSpace(tpn.GetString())
                    ? tpn.GetString()!
                    : "Name";
                var title = input.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? string.Empty : string.Empty;

                var properties = new Dictionary<string, object>
                {
                    [titlePropertyName] = new
                    {
                        title = new[] { new { text = new { content = title } } }
                    }
                };

                if (input.TryGetProperty("properties", out var extraProps) && extraProps.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in extraProps.EnumerateObject())
                    {
                        if (prop.NameEquals(titlePropertyName)) continue;
                        var value = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
                        properties[prop.Name] = new
                        {
                            rich_text = new[] { new { text = new { content = value ?? string.Empty } } }
                        };
                    }
                }

                var body = new
                {
                    parent = new { database_id = databaseId },
                    properties
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/pages")
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Notion Error", response.StatusCode, content);
                }

                var result = JsonSerializer.Deserialize<CreatePageResponse>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = result?.Id,
                    url = result?.Url
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

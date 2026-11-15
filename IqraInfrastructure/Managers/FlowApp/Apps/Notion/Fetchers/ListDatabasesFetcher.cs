using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Notion.Models;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Notion.Fetchers
{
    public class ListDatabasesFetcher : IFlowDataFetcher
    {
        private readonly NotionApp _app;
        public ListDatabasesFetcher(NotionApp app) { _app = app; }

        public string FetcherKey => "ListDatabases";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var token = integration.DecryptedFields["Token"];
                var client = _app.CreateClient(token);

                var options = new List<DynamicOption>();
                string? cursor = null;
                int guard = 0;

                do
                {
                    var request = new SearchRequest
                    {
                        Filter = new SearchFilter { Value = "database", Property = "object" },
                        PageSize = 100,
                        StartCursor = cursor
                    };

                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/search")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                    };

                    var response = await client.SendAsync(httpRequest);
                    if (!response.IsSuccessStatusCode) break;

                    var result = JsonSerializer.Deserialize<SearchResponse>(await response.Content.ReadAsStringAsync());
                    if (result == null) break;

                    foreach (var db in result.Results)
                    {
                        var name = db.Title != null && db.Title.Count > 0
                            ? string.Concat(db.Title.Select(t => t.PlainText))
                            : "(Untitled)";

                        options.Add(new DynamicOption
                        {
                            Label = string.IsNullOrWhiteSpace(name) ? "(Untitled)" : name,
                            Value = db.Id,
                            Description = db.Id
                        });
                    }

                    cursor = result.HasMore ? result.NextCursor : null;
                    guard++;
                }
                while (!string.IsNullOrEmpty(cursor) && guard < 10);

                return options;
            }
            catch { return new(); }
        }
    }
}

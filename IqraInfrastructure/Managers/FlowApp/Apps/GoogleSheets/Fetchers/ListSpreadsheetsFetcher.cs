using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleSheets.Fetchers
{
    public class ListSpreadsheetsFetcher : IFlowDataFetcher
    {
        private readonly GoogleSheetsApp _app;
        public ListSpreadsheetsFetcher(GoogleSheetsApp app) { _app = app; }

        public string FetcherKey => "ListSpreadsheets";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var accessToken = integration.OAuthAccessToken!;
                var client = _app.CreateClient(accessToken);

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["q"] = "mimeType='application/vnd.google-apps.spreadsheet' and trashed=false";
                query["fields"] = "files(id,name)";
                query["pageSize"] = "100";
                query["orderBy"] = "modifiedTime desc";

                var response = await client.GetAsync($"https://www.googleapis.com/drive/v3/files?{query}");
                if (!response.IsSuccessStatusCode) return new();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var options = new List<DynamicOption>();
                if (doc.RootElement.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                {
                    foreach (var file in files.EnumerateArray())
                    {
                        var id = file.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        var name = file.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                        if (string.IsNullOrEmpty(id)) continue;
                        options.Add(new DynamicOption { Label = name ?? id, Value = id, Description = id });
                    }
                }
                return options;
            }
            catch { return new(); }
        }
    }
}

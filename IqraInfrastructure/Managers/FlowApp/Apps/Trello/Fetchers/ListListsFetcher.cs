using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Trello.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Trello.Fetchers
{
    public class ListListsFetcher : IFlowDataFetcher
    {
        private readonly TrelloApp _app;
        public ListListsFetcher(TrelloApp app) { _app = app; }

        public string FetcherKey => "ListLists";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            if (context.ValueKind != JsonValueKind.Object ||
                !context.TryGetProperty("boardId", out var boardProp) ||
                string.IsNullOrWhiteSpace(boardProp.GetString()))
            {
                return new();
            }

            var boardId = boardProp.GetString()!;

            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var token = integration.DecryptedFields["Token"];
                var client = _app.CreateClient();

                var path = $"/1/boards/{Uri.EscapeDataString(boardId)}/lists?filter=open&fields=id,name,closed&{TrelloApp.BuildAuthQuery(apiKey, token)}";
                var response = await client.GetAsync(path);
                if (!response.IsSuccessStatusCode) return new();

                var lists = await response.Content.ReadFromJsonAsync<List<TrelloList>>();

                return lists?
                    .Where(l => !l.Closed)
                    .Select(l => new DynamicOption { Label = l.Name, Value = l.Id, Description = l.Id })
                    .ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

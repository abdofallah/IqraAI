using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Trello.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Trello.Fetchers
{
    public class ListBoardsFetcher : IFlowDataFetcher
    {
        private readonly TrelloApp _app;
        public ListBoardsFetcher(TrelloApp app) { _app = app; }

        public string FetcherKey => "ListBoards";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var token = integration.DecryptedFields["Token"];
                var client = _app.CreateClient();

                var path = $"/1/members/me/boards?filter=open&fields=id,name,closed&{TrelloApp.BuildAuthQuery(apiKey, token)}";
                var response = await client.GetAsync(path);
                if (!response.IsSuccessStatusCode) return new();

                var boards = await response.Content.ReadFromJsonAsync<List<TrelloBoard>>();

                return boards?
                    .Where(b => !b.Closed)
                    .Select(b => new DynamicOption { Label = b.Name, Value = b.Id, Description = b.Id })
                    .ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

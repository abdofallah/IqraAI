using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Fetchers
{
    public class ListBasesFetcher : IFlowDataFetcher
    {
        private readonly AirtableApp _app;
        public ListBasesFetcher(AirtableApp app) { _app = app; }

        public string FetcherKey => "ListBases";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var accessToken = integration.DecryptedFields["AccessToken"];
                var client = _app.CreateClient(accessToken);

                var options = new List<DynamicOption>();
                string? offset = null;
                int guard = 0;

                do
                {
                    var url = "/v0/meta/bases";
                    if (!string.IsNullOrEmpty(offset))
                        url += $"?offset={Uri.EscapeDataString(offset)}";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) break;

                    var result = await response.Content.ReadFromJsonAsync<ListBasesResponse>();
                    if (result == null) break;

                    options.AddRange(result.Bases.Select(b => new DynamicOption
                    {
                        Label = b.Name,
                        Value = b.Id,
                        Description = b.Id
                    }));

                    offset = result.Offset;
                    guard++;
                }
                while (!string.IsNullOrEmpty(offset) && guard < 10);

                return options;
            }
            catch { return new(); }
        }
    }
}

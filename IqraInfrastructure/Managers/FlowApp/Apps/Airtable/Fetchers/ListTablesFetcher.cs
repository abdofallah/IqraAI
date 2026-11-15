using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Fetchers
{
    public class ListTablesFetcher : IFlowDataFetcher
    {
        private readonly AirtableApp _app;
        public ListTablesFetcher(AirtableApp app) { _app = app; }

        public string FetcherKey => "ListTables";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            if (context.ValueKind != JsonValueKind.Object ||
                !context.TryGetProperty("baseId", out var baseProp) ||
                string.IsNullOrWhiteSpace(baseProp.GetString()))
            {
                return new();
            }

            var baseId = baseProp.GetString()!;

            try
            {
                var accessToken = integration.DecryptedFields["AccessToken"];
                var client = _app.CreateClient(accessToken);

                var response = await client.GetAsync($"/v0/meta/bases/{baseId}/tables");
                if (!response.IsSuccessStatusCode) return new();

                var result = await response.Content.ReadFromJsonAsync<ListTablesResponse>();

                return result?.Tables.Select(t => new DynamicOption
                {
                    Label = t.Name,
                    Value = t.Name,
                    Description = t.Id
                }).ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

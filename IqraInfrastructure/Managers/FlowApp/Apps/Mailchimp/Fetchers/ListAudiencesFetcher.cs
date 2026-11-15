using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Mailchimp.Fetchers
{
    public class ListAudiencesFetcher : IFlowDataFetcher
    {
        private readonly MailchimpApp _app;
        public ListAudiencesFetcher(MailchimpApp app) { _app = app; }

        public string FetcherKey => "ListAudiences";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient(apiKey);

                var response = await client.GetAsync("/3.0/lists?count=1000&fields=lists.id,lists.name");
                if (!response.IsSuccessStatusCode) return new();

                var result = await response.Content.ReadFromJsonAsync<ListsResponse>();

                return result?.Lists.Select(l => new DynamicOption
                {
                    Label = l.Name,
                    Value = l.Id,
                    Description = l.Id
                }).ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

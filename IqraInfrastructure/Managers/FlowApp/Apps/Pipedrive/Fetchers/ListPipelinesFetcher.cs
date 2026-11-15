using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Pipedrive.Fetchers
{
    public class ListPipelinesFetcher : IFlowDataFetcher
    {
        private readonly PipedriveApp _app;
        public ListPipelinesFetcher(PipedriveApp app) { _app = app; }

        public string FetcherKey => "ListPipelines";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var apiToken = integration.DecryptedFields["ApiToken"];
                var client = _app.CreateClient();

                var response = await client.GetAsync(PipedriveApp.BuildPath("/v1/pipelines", apiToken));
                if (!response.IsSuccessStatusCode) return new();

                var result = await response.Content.ReadFromJsonAsync<PipedriveResponse<List<PipelineData>>>();

                return result?.Data?
                    .Where(p => p.Active)
                    .Select(p => new DynamicOption
                    {
                        Label = p.Name,
                        Value = p.Id,
                        Description = $"ID: {p.Id}"
                    }).ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

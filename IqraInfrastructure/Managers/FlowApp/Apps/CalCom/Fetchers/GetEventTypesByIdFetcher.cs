using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Fetchers
{
    public class GetEventTypesByIdFetcher : IFlowDataFetcher
    {
        private readonly CalComApp _app;
        public GetEventTypesByIdFetcher(CalComApp app) { _app = app; }

        public string FetcherKey => "GetEventTypesById";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/v2/event-types");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-06-14");
                var response = await client.SendAsync(httpRequest);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception(error);
                }

                var result = await response.Content.ReadFromJsonAsync<CalComResponse<List<EventTypeDto>>>();

                return result?.Data?.Select(e => new DynamicOption
                {
                    Label = $"{e.Title} ({e.Length}m)",
                    Value = e.Id,
                    Description = $"ID: {e.Id}"
                }).ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}
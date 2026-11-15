using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Fetchers
{
    public class ListCalendarsFetcher : IFlowDataFetcher
    {
        private readonly GoogleCalendarApp _app;
        public ListCalendarsFetcher(GoogleCalendarApp app) { _app = app; }

        public string FetcherKey => "ListCalendars";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var accessToken = integration.OAuthAccessToken!;
                var client = _app.CreateClient(accessToken);

                var response = await client.GetAsync("/calendar/v3/users/me/calendarList");
                if (!response.IsSuccessStatusCode) return new();

                var result = await response.Content.ReadFromJsonAsync<CalendarListResponse>();
                return result?.Items.Select(c => new DynamicOption
                {
                    Label = c.Primary ? $"{c.Summary} (Primary)" : c.Summary ?? c.Id,
                    Value = c.Id,
                    Description = c.Id
                }).ToList() ?? new();
            }
            catch { return new(); }
        }
    }
}

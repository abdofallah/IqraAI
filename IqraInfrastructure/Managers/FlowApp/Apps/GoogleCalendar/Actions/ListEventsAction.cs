using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Models;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Actions
{
    public partial class ListEventsAction : IFlowAction
    {
        private readonly GoogleCalendarApp _app;

        public ListEventsAction(GoogleCalendarApp app)
        {
            _app = app;
        }

        public string ActionKey => "ListEvents";
        public string Name => "List Events";
        public string Description => "Lists upcoming calendar events within an optional time window. Useful to check availability before booking.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel? integration)
        {
            try
            {
                var accessToken = integration!.OAuthAccessToken!;
                var client = _app.CreateClient(accessToken);

                var calendarId = input.TryGetProperty("calendarId", out var cal) && !string.IsNullOrWhiteSpace(cal.GetString())
                    ? cal.GetString()!
                    : "primary";

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["singleEvents"] = "true";
                query["orderBy"] = "startTime";
                query["maxResults"] = input.TryGetProperty("maxResults", out var mr) && mr.ValueKind == JsonValueKind.Number
                    ? mr.GetInt32().ToString()
                    : "10";

                if (input.TryGetProperty("timeMin", out var tmin) && !string.IsNullOrWhiteSpace(tmin.GetString()))
                {
                    query["timeMin"] = tmin.GetString();
                }
                else
                {
                    query["timeMin"] = DateTime.UtcNow.ToString("o");
                }

                if (input.TryGetProperty("timeMax", out var tmax) && !string.IsNullOrWhiteSpace(tmax.GetString()))
                {
                    query["timeMax"] = tmax.GetString();
                }

                var url = $"/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events?{query}";
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Google Calendar error: {response.StatusCode} - {content}");
                }

                var list = JsonSerializer.Deserialize<EventListResponse>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    count = list?.Items.Count ?? 0,
                    events = list?.Items.Select(e => new
                    {
                        id = e.Id,
                        summary = e.Summary,
                        start = e.Start?.DateTime,
                        end = e.End?.DateTime,
                        htmlLink = e.HtmlLink
                    })
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

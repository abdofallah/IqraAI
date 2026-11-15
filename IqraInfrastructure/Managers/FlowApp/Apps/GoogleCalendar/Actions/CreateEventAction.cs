using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.GoogleCalendar.Actions
{
    public partial class CreateEventAction : IFlowAction
    {
        private readonly GoogleCalendarApp _app;

        public CreateEventAction(GoogleCalendarApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateEvent";
        public string Name => "Create Event";
        public string Description => "Creates a calendar event. Ideal for booking an appointment captured during a call.";

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

                var timeZone = input.TryGetProperty("timeZone", out var tz) && !string.IsNullOrWhiteSpace(tz.GetString())
                    ? tz.GetString()
                    : "UTC";

                var request = new CreateEventRequest
                {
                    Summary = input.GetProperty("summary").GetString() ?? string.Empty,
                    Description = input.TryGetProperty("description", out var d) ? d.GetString() : null,
                    Location = input.TryGetProperty("location", out var l) ? l.GetString() : null,
                    Start = new EventDateTime { DateTime = input.GetProperty("startDateTime").GetString(), TimeZone = timeZone },
                    End = new EventDateTime { DateTime = input.GetProperty("endDateTime").GetString(), TimeZone = timeZone }
                };

                if (input.TryGetProperty("attendeeEmail", out var att) && !string.IsNullOrWhiteSpace(att.GetString()))
                {
                    request.Attendees = new List<EventAttendee> { new EventAttendee { Email = att.GetString()! } };
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Google Calendar error", response.StatusCode, content);
                }

                var created = JsonSerializer.Deserialize<CalendarEvent>(content);
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = created?.Id,
                    htmlLink = created?.HtmlLink,
                    status = created?.Status,
                    summary = created?.Summary
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

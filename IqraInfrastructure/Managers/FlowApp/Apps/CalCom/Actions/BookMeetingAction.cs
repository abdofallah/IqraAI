using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class BookMeetingAction : IFlowAction
    {
        private readonly CalComApp _app;

        public BookMeetingAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "BookMeeting";
        public string Name => "Book a Meeting";
        public string Description => "Schedules a new booking on Cal.com.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success (201)" },
                new ActionOutputPort { Key = "conflict", Label = "Slot Taken (409)" }, // Specific path for re-booking logic
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel integration)
        {
            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                // 1. Construct Request Object
                // We map the flat input schema to the nested structure Cal.com expects
                var request = new CreateBookingRequest
                {
                    Start = input.GetProperty("start").GetString() ?? string.Empty,
                    Attendee = new Attendee
                    {
                        Name = input.GetProperty("attendeeName").GetString() ?? "Guest",
                        Email = input.GetProperty("attendeeEmail").GetString() ?? "",
                        TimeZone = input.TryGetProperty("attendeeTimeZone", out var tz) ? tz.GetString() ?? "UTC" : "UTC",
                        PhoneNumber = input.TryGetProperty("attendeePhone", out var ph) ? ph.GetString() : null,
                        Language = "en"
                    },
                    Metadata = new Dictionary<string, string>()
                };

                // Add Notes to metadata if present
                if (input.TryGetProperty("notes", out var notes))
                {
                    request.Metadata.Add("notes", notes.GetString() ?? "");
                }

                // 2. Handle Polymorphic Targeting (ID vs Slug)
                if (input.TryGetProperty("eventTypeId", out var id))
                {
                    request.EventTypeId = id.GetInt32();
                }
                else if (input.TryGetProperty("teamSlug", out var team))
                {
                    request.TeamSlug = team.GetString();
                    request.EventTypeSlug = input.GetProperty("eventTypeSlug").GetString();
                }
                else
                {
                    request.Username = input.GetProperty("username").GetString();
                    request.EventTypeSlug = input.GetProperty("eventTypeSlug").GetString();
                }

                // 3. Execute
                // Note: Cal.com uses POST /bookings
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/bookings");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // 4. Handle Outcomes

                // Success
                if (response.IsSuccessStatusCode)
                {
                    var resultData = JsonSerializer.Deserialize<CalComResponse<BookingResponseData>>(responseContent);
                    return ActionExecutionResult.SuccessPort("success", resultData?.Data);
                }

                // Slot Conflict (Very common in Voice AI)
                // If the user says "Book 9am", but 9am gets taken while they were speaking.
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return ActionExecutionResult.SuccessPort("conflict", new { message = "Time slot is no longer available." });
                }

                // General Error
                return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {responseContent}");
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
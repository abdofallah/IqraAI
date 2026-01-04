using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class MarkAbsentAction : IFlowAction
    {
        private readonly CalComApp _app;

        public MarkAbsentAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "MarkAbsent";
        public string Name => "Mark Absent";
        public string Description => "Marks a host or attendee as absent for a past booking.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel integration)
        {
            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                // 1. Get UID
                if (!input.TryGetProperty("bookingUid", out var uidProp) || string.IsNullOrWhiteSpace(uidProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Booking UID is required.");
                }
                var uid = uidProp.GetString();

                // 2. Construct Payload
                var request = new MarkAbsentRequest();

                // Check if marking host
                if (input.TryGetProperty("markHostAbsent", out var hostProp) && hostProp.GetBoolean())
                {
                    request.IsHost = true;
                }
                else
                {
                    // Marking an attendee
                    request.IsHost = false;

                    if (input.TryGetProperty("attendeeEmail", out var emailProp) && !string.IsNullOrWhiteSpace(emailProp.GetString()))
                    {
                        request.Attendees.Add(new AbsentAttendeePayload
                        {
                            Email = emailProp.GetString()!,
                            Absent = true
                        });
                    }
                    else
                    {
                        return ActionExecutionResult.Failure("VALIDATION_ERROR", "Attendee Email is required when not marking host absent.");
                    }
                }

                // 3. Execute
                // POST /v2/bookings/{uid}/mark-absent
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/v2/bookings/{uid}/mark-absent");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
                }

                // 4. Return Data
                var resultData = JsonSerializer.Deserialize<CalComResponse<BookingResponseData>>(content);

                return ActionExecutionResult.SuccessPort("success", resultData?.Data);
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
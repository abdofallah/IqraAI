using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class RescheduleBookingAction : IFlowAction
    {
        private readonly CalComApp _app;

        public RescheduleBookingAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "RescheduleBooking";
        public string Name => "Reschedule Booking";
        public string Description => "Moves an existing booking to a new time slot.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success (201)" },
                new ActionOutputPort { Key = "conflict", Label = "Slot Taken (409)" },
                new ActionOutputPort { Key = "not_found", Label = "Original Not Found (404)" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel integration)
        {
            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                // 1. Validate Inputs
                if (!input.TryGetProperty("bookingUid", out var uidProp) || string.IsNullOrWhiteSpace(uidProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Booking UID is required.");
                }
                var uid = uidProp.GetString();

                if (!input.TryGetProperty("newStartTime", out var timeProp) || string.IsNullOrWhiteSpace(timeProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "New Start Time is required.");
                }

                // 2. Build Payload
                var request = new RescheduleBookingRequest
                {
                    Start = timeProp.GetString()!,
                    Reason = input.TryGetProperty("reason", out var reason)
                        ? reason.GetString() ?? "Requested by User"
                        : "Requested by User"
                };

                // 3. Execute
                // POST /v2/bookings/{uid}/reschedule
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/v2/bookings/{uid}/reschedule");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                // 4. Handle Logic Branches

                // Success
                if (response.IsSuccessStatusCode)
                {
                    var resultData = JsonSerializer.Deserialize<CalComResponse<BookingResponseData>>(content);
                    // The API returns the NEW booking object.
                    return ActionExecutionResult.SuccessPort("success", resultData?.Data);
                }

                // Conflict (New slot is busy)
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    return ActionExecutionResult.SuccessPort("conflict", new { message = "The selected new time slot is unavailable." });
                }

                // Not Found (Old booking invalid)
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return ActionExecutionResult.SuccessPort("not_found", new { message = "Original booking UID not found." });
                }

                // General Error
                return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
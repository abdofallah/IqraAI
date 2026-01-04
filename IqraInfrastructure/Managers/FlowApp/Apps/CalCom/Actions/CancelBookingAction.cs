using IqraCore.Entities.Business;
using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class CancelBookingAction : IFlowAction
    {
        private readonly CalComApp _app;

        public CancelBookingAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "CancelBooking";
        public string Name => "Cancel Booking";
        public string Description => "Cancels an existing booking.";

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

                // 2. Prepare Request Body
                var request = new CancelBookingRequest
                {
                    Reason = input.TryGetProperty("cancellationReason", out var reason)
                        ? reason.GetString() ?? "Requested by User"
                        : "Requested by User"
                };

                // 3. Execute
                // POST /v2/bookings/{uid}/cancel
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/v2/bookings/{uid}/cancel");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Handle 404 Not Found specifically? 
                    // Usually implies the booking didn't exist or was already cancelled.
                    return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
                }

                // 4. Return Data
                // We return the raw response data so the agent can confirm details (e.g. "Cancelled meeting with Jane")
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
using Hume.Core.Async;
using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Net;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class GetBookingAction : IFlowAction
    {
        private readonly CalComApp _app;

        public GetBookingAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "GetBooking";
        public string Name => "Get Booking";
        public string Description => "Retrieves details of a specific booking by UID.";

        public IReadOnlyList<ActionOutputPort> GetOutputPorts()
        {
            return new List<ActionOutputPort>
            {
                new ActionOutputPort { Key = "success", Label = "Success" },
                new ActionOutputPort { Key = "not_found", Label = "Not Found (404)" },
                new ActionOutputPort { Key = "error", Label = "Error" }
            };
        }

        public async Task<ActionExecutionResult> ExecuteAsync(JsonElement input, BusinessAppIntegrationDecryptedModel integration)
        {
            try
            {
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                // 1. Validate Input
                if (!input.TryGetProperty("bookingUid", out var uidProp) || string.IsNullOrWhiteSpace(uidProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Booking UID is required.");
                }
                var uid = uidProp.GetString();

                // 2. Call API
                // GET /v2/bookings/{uid}
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/bookings/{uid}");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                // 3. Handle 404 specifically
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return ActionExecutionResult.SuccessPort("not_found", new { message = "Booking not found." });
                }

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
                }

                // 4. Parse Data
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
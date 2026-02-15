using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class AddGuestsAction : IFlowAction
    {
        private readonly CalComApp _app;

        public AddGuestsAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "AddGuests";
        public string Name => "Add Guest";
        public string Description => "Adds a new attendee to an existing booking.";

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

                // 1. Validate Input
                if (!input.TryGetProperty("bookingUid", out var uidProp) || string.IsNullOrWhiteSpace(uidProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Booking UID is required.");
                }
                var uid = uidProp.GetString();

                if (!input.TryGetProperty("guestEmail", out var emailProp) || string.IsNullOrWhiteSpace(emailProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Guest Email is required.");
                }

                // 2. Construct Payload
                // The API expects a list of guests. We wrap our single input into that list.
                var request = new AddGuestsRequest();

                var guest = new Guest
                {
                    Email = emailProp.GetString()!,
                    Name = input.TryGetProperty("guestName", out var nameProp) ? nameProp.GetString() ?? "" : "",
                    TimeZone = input.TryGetProperty("guestTimeZone", out var tzProp) ? tzProp.GetString() ?? "UTC" : "UTC"
                };

                request.Guests.Add(guest);

                // 3. Execute
                // POST /v2/bookings/{uid}/guests
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/v2/bookings/{uid}/guests");
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, "application/json");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-06-14");
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
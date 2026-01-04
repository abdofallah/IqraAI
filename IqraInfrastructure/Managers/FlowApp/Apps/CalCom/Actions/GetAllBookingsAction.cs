using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class GetAllBookingsAction : IFlowAction
    {
        private readonly CalComApp _app;

        public GetAllBookingsAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "GetAllBookings";
        public string Name => "Get All Bookings";
        public string Description => "Retrieves a list of bookings filtered by status, email, or date.";

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

                // 1. Build Query String
                var query = HttpUtility.ParseQueryString(string.Empty);

                // Status (handle comma separation if user types "upcoming,past")
                if (input.TryGetProperty("status", out var statusProp) && !string.IsNullOrWhiteSpace(statusProp.GetString()))
                {
                    var statuses = statusProp.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in statuses)
                    {
                        query.Add("status", s.Trim());
                    }
                }

                // Filters
                if (input.TryGetProperty("attendeeEmail", out var emailProp) && !string.IsNullOrWhiteSpace(emailProp.GetString()))
                {
                    query["attendeeEmail"] = emailProp.GetString();
                }

                if (input.TryGetProperty("afterStart", out var startProp) && !string.IsNullOrWhiteSpace(startProp.GetString()))
                {
                    query["afterStart"] = startProp.GetString();
                }

                if (input.TryGetProperty("beforeEnd", out var endProp) && !string.IsNullOrWhiteSpace(endProp.GetString()))
                {
                    query["beforeEnd"] = endProp.GetString();
                }

                // Pagination (Limit)
                int limit = 5;
                if (input.TryGetProperty("limit", out var limitProp))
                {
                    limit = limitProp.GetInt32();
                }
                query["take"] = limit.ToString();

                // 2. Call API
                // GET /v2/bookings?status=upcoming&take=5...
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/bookings?{query}");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-08-13");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
                }

                // 3. Parse and Return
                var resultData = JsonSerializer.Deserialize<CalComResponse<List<BookingResponseData>>>(content);

                if (resultData?.Data == null)
                {
                    return ActionExecutionResult.SuccessPort("success", new List<BookingResponseData>());
                }

                // We return the list directly. The Agent can iterate using {{ for item in tool.result }}
                return ActionExecutionResult.SuccessPort("success", resultData.Data);
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
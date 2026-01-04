using IqraCore.Entities.Business;
using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Models;
using System.Text.Json;
using System.Web;

namespace IqraInfrastructure.Managers.FlowApp.Apps.CalCom.Actions
{
    public partial class GetSlotsAction : IFlowAction
    {
        private readonly CalComApp _app;

        public GetSlotsAction(CalComApp app)
        {
            _app = app;
        }

        public string ActionKey => "GetSlots";
        public string Name => "Get Available Slots";
        public string Description => "Retrieves available time slots for a specific event type.";

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
                // 1. Setup Client
                var apiKey = integration.DecryptedFields["ApiKey"];
                var client = _app.CreateClient();

                // 2. Build Query Parameters
                var query = HttpUtility.ParseQueryString(string.Empty);

                query["start"] = input.GetProperty("startTime").GetString();
                query["end"] = input.GetProperty("endTime").GetString();

                if (input.TryGetProperty("timeZone", out var tz))
                    query["timeZone"] = tz.GetString();

                // Handle Polymorphism (ID vs Slug)
                if (input.TryGetProperty("eventTypeId", out var id))
                {
                    query["eventTypeId"] = id.ToString();
                }
                else
                {
                    query["username"] = input.GetProperty("username").GetString();
                    query["eventTypeSlug"] = input.GetProperty("eventTypeSlug").GetString();
                }

                // 3. Call API
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/slots?{query}");
                httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
                httpRequest.Headers.Add("cal-api-version", "2024-09-04");
                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Cal.com Error: {response.StatusCode} - {content}");
                }

                // 4. Parse & Flatten
                // The API returns: { "data": { "2024-01-01": [ { "start": "..." } ] } }
                var result = JsonSerializer.Deserialize<CalComResponse<Dictionary<string, List<Slot>>>>(content);

                if (result?.Data == null)
                {
                    return ActionExecutionResult.SuccessPort("success", new List<string>());
                }

                // Flatten the dictionary into a simple list of ISO strings for the Agent
                var flatSlots = result.Data
                    .SelectMany(kvp => kvp.Value)
                    .Select(s => s.Start)
                    .OrderBy(s => s)
                    .ToList();

                // Return structure: { "count": 5, "slots": ["2024...", "2024..."] }
                return ActionExecutionResult.SuccessPort("success", new
                {
                    count = flatSlots.Count,
                    slots = flatSlots
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}
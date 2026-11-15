using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.OutlookCalendar.Actions
{
    public partial class CreateEventAction : IFlowAction
    {
        private readonly OutlookCalendarApp _app;

        public CreateEventAction(OutlookCalendarApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateEvent";
        public string Name => "Create Event";
        public string Description => "Creates an event on the connected Outlook / Microsoft 365 calendar.";

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

                var timeZone = input.TryGetProperty("timeZone", out var tz) && !string.IsNullOrWhiteSpace(tz.GetString())
                    ? tz.GetString()!
                    : "UTC";

                var isHtml = input.TryGetProperty("isHtml", out var h) && h.ValueKind == JsonValueKind.True;

                var body = new Dictionary<string, object?>
                {
                    ["subject"] = input.GetProperty("subject").GetString(),
                    ["body"] = new Dictionary<string, object?>
                    {
                        ["contentType"] = isHtml ? "HTML" : "Text",
                        ["content"] = input.TryGetProperty("body", out var b) ? b.GetString() ?? string.Empty : string.Empty
                    },
                    ["start"] = new Dictionary<string, object?>
                    {
                        ["dateTime"] = input.GetProperty("startDateTime").GetString(),
                        ["timeZone"] = timeZone
                    },
                    ["end"] = new Dictionary<string, object?>
                    {
                        ["dateTime"] = input.GetProperty("endDateTime").GetString(),
                        ["timeZone"] = timeZone
                    }
                };

                if (input.TryGetProperty("location", out var loc) && !string.IsNullOrWhiteSpace(loc.GetString()))
                {
                    body["location"] = new Dictionary<string, object?> { ["displayName"] = loc.GetString() };
                }

                if (input.TryGetProperty("attendeeEmail", out var att) && !string.IsNullOrWhiteSpace(att.GetString()))
                {
                    body["attendees"] = new[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["emailAddress"] = new Dictionary<string, object?> { ["address"] = att.GetString() },
                            ["type"] = "required"
                        }
                    };
                }

                var payload = JsonSerializer.Serialize(body);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1.0/me/events")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Microsoft Graph error: {response.StatusCode} - {content}");
                }

                using var doc = JsonDocument.Parse(content);
                var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                var webLink = doc.RootElement.TryGetProperty("webLink", out var wl) ? wl.GetString() : null;

                return ActionExecutionResult.SuccessPort("success", new { id, webLink });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Zoom.Actions
{
    public partial class CreateMeetingAction : IFlowAction
    {
        private readonly ZoomApp _app;

        public CreateMeetingAction(ZoomApp app)
        {
            _app = app;
        }

        public string ActionKey => "CreateMeeting";
        public string Name => "Create Meeting";
        public string Description => "Schedules a Zoom meeting for the connected user and returns the join URL.";

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

                var topic = input.TryGetProperty("topic", out var t) && !string.IsNullOrWhiteSpace(t.GetString())
                    ? t.GetString()!
                    : "Meeting";

                var hasStart = input.TryGetProperty("startTime", out var st) && !string.IsNullOrWhiteSpace(st.GetString());
                var body = new Dictionary<string, object?>
                {
                    ["topic"] = topic,
                    ["type"] = hasStart ? 2 : 1
                };

                if (hasStart)
                {
                    body["start_time"] = st.GetString();
                    if (input.TryGetProperty("timeZone", out var tz) && !string.IsNullOrWhiteSpace(tz.GetString()))
                    {
                        body["timezone"] = tz.GetString();
                    }
                }

                if (input.TryGetProperty("durationMinutes", out var dur) && dur.ValueKind == JsonValueKind.Number)
                {
                    body["duration"] = dur.GetInt32();
                }

                if (input.TryGetProperty("agenda", out var ag) && !string.IsNullOrWhiteSpace(ag.GetString()))
                {
                    body["agenda"] = ag.GetString();
                }

                var payload = JsonSerializer.Serialize(body);
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/users/me/meetings")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ActionExecutionResult.Failure("API_ERROR", $"Zoom error: {response.StatusCode} - {content}");
                }

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                return ActionExecutionResult.SuccessPort("success", new
                {
                    id = root.TryGetProperty("id", out var idEl) ? (idEl.ValueKind == JsonValueKind.Number ? idEl.GetInt64().ToString() : idEl.GetString()) : null,
                    joinUrl = root.TryGetProperty("join_url", out var ju) ? ju.GetString() : null,
                    startUrl = root.TryGetProperty("start_url", out var su) ? su.GetString() : null,
                    password = root.TryGetProperty("password", out var pw) ? pw.GetString() : null
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Slack.Models;
using IqraInfrastructure.Helpers.Integrations;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Slack.Actions
{
    public partial class SendMessageAction : IFlowAction
    {
        private readonly SlackApp _app;

        public SendMessageAction(SlackApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendMessage";
        public string Name => "Send Channel Message";
        public string Description => "Posts a message to a Slack channel via chat.postMessage.";

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
                var botToken = integration!.DecryptedFields["BotToken"];
                var client = _app.CreateClient(botToken);

                var request = new PostMessageRequest
                {
                    Channel = input.GetProperty("channel").GetString() ?? string.Empty,
                    Text = input.GetProperty("text").GetString() ?? string.Empty,
                    ThreadTs = input.TryGetProperty("threadTs", out var ts) && !string.IsNullOrWhiteSpace(ts.GetString()) ? ts.GetString() : null
                };

                if (string.IsNullOrWhiteSpace(request.Channel))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "Channel is required.");
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat.postMessage")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(httpRequest);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ProviderReplyHelper.ActionFailure("Slack HTTP Error", response.StatusCode, content);
                }

                var result = JsonSerializer.Deserialize<PostMessageResponse>(content);

                if (result == null || !result.Ok)
                {
                    return ActionExecutionResult.Failure("SLACK_ERROR", $"Slack rejected the message: {result?.Error ?? "unknown_error"}");
                }

                return ActionExecutionResult.SuccessPort("success", new
                {
                    channel = result.Channel,
                    ts = result.Ts
                });
            }
            catch (Exception ex)
            {
                return ActionExecutionResult.Failure("EXCEPTION", ex.Message);
            }
        }
    }
}

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Slack.Models;
using System.Text;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Slack.Actions
{
    public partial class SendDirectMessageAction : IFlowAction
    {
        private readonly SlackApp _app;

        public SendDirectMessageAction(SlackApp app)
        {
            _app = app;
        }

        public string ActionKey => "SendDirectMessage";
        public string Name => "Send Direct Message";
        public string Description => "Opens a DM with a user (by Slack user ID) and posts a message to it.";

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

                if (!input.TryGetProperty("userId", out var userProp) || string.IsNullOrWhiteSpace(userProp.GetString()))
                {
                    return ActionExecutionResult.Failure("VALIDATION_ERROR", "User ID is required.");
                }

                // 1. Open (or reuse) the DM conversation to resolve a channel ID.
                var openRequest = new OpenConversationRequest { Users = userProp.GetString()! };
                var openHttp = new HttpRequestMessage(HttpMethod.Post, "/api/conversations.open")
                {
                    Content = new StringContent(JsonSerializer.Serialize(openRequest), Encoding.UTF8, "application/json")
                };
                var openResponse = await client.SendAsync(openHttp);
                var openContent = await openResponse.Content.ReadAsStringAsync();

                var opened = JsonSerializer.Deserialize<OpenConversationResponse>(openContent);
                if (opened == null || !opened.Ok || opened.Channel == null)
                {
                    return ActionExecutionResult.Failure("SLACK_ERROR", $"Could not open DM: {opened?.Error ?? "unknown_error"}");
                }

                // 2. Post the message into that DM channel.
                var postRequest = new PostMessageRequest
                {
                    Channel = opened.Channel.Id,
                    Text = input.GetProperty("text").GetString() ?? string.Empty
                };
                var postHttp = new HttpRequestMessage(HttpMethod.Post, "/api/chat.postMessage")
                {
                    Content = new StringContent(JsonSerializer.Serialize(postRequest), Encoding.UTF8, "application/json")
                };
                var postResponse = await client.SendAsync(postHttp);
                var postContent = await postResponse.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<PostMessageResponse>(postContent);
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

using IqraCore.Entities.FlowApp;
using IqraCore.Interfaces.FlowApp;
using IqraCore.Models.FlowApp.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Slack.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Slack.Fetchers
{
    public class ListChannelsFetcher : IFlowDataFetcher
    {
        private readonly SlackApp _app;
        public ListChannelsFetcher(SlackApp app) { _app = app; }

        public string FetcherKey => "ListChannels";

        public async Task<List<DynamicOption>> FetchOptionsAsync(BusinessAppIntegrationDecryptedModel? integration, JsonElement context)
        {
            if (integration == null) return new();

            try
            {
                var botToken = integration.DecryptedFields["BotToken"];
                var client = _app.CreateClient(botToken);

                var options = new List<DynamicOption>();
                string? cursor = null;
                int guard = 0;

                do
                {
                    var url = "/api/conversations.list?types=public_channel,private_channel&exclude_archived=true&limit=200";
                    if (!string.IsNullOrEmpty(cursor))
                        url += $"&cursor={Uri.EscapeDataString(cursor)}";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) break;

                    var result = await response.Content.ReadFromJsonAsync<ListChannelsResponse>();
                    if (result == null || !result.Ok) break;

                    foreach (var channel in result.Channels.Where(c => !c.IsArchived))
                    {
                        options.Add(new DynamicOption
                        {
                            Label = $"#{channel.Name}",
                            Value = channel.Id,
                            Description = channel.IsPrivate ? "Private channel" : "Public channel"
                        });
                    }

                    cursor = result.ResponseMetadata?.NextCursor;
                    guard++;
                }
                while (!string.IsNullOrEmpty(cursor) && guard < 10);

                return options.OrderBy(o => o.Label).ToList();
            }
            catch { return new(); }
        }
    }
}

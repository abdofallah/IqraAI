using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Slack.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Slack.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Slack
{
    public class SlackApp : IFlowApp
    {
        public string AppKey => "slack";
        public string Name => "Slack";
        public string IconUrl => "https://cdn.simpleicons.org/slack";
        public string? IntegrationType => "slack"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SlackApp> _logger;

        private const string BaseUrl = "https://slack.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public SlackApp(IHttpClientFactory httpClientFactory, ILogger<SlackApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendMessageAction(this),
                new SendDirectMessageAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListChannelsFetcher(this)
            };
        }

        public HttpClient CreateClient(string botToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", botToken);
            return client;
        }
    }
}

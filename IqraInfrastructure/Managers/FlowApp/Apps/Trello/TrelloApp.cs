using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Trello.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Trello.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Trello
{
    public class TrelloApp : IFlowApp
    {
        public string AppKey => "trello";
        public string Name => "Trello";
        public string IconUrl => "https://cdn.simpleicons.org/trello";
        public string? IntegrationType => "trello"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TrelloApp> _logger;

        private const string BaseUrl = "https://api.trello.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public TrelloApp(IHttpClientFactory httpClientFactory, ILogger<TrelloApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateCardAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListBoardsFetcher(this),
                new ListListsFetcher(this)
            };
        }

        public HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            return client;
        }

        public static string BuildAuthQuery(string apiKey, string token)
        {
            return $"key={Uri.EscapeDataString(apiKey)}&token={Uri.EscapeDataString(token)}";
        }
    }
}

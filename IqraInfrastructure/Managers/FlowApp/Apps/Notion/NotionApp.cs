using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Notion.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Notion.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Notion
{
    public class NotionApp : IFlowApp
    {
        public string AppKey => "notion";
        public string Name => "Notion";
        public string IconUrl => "https://cdn.simpleicons.org/notion/000000";
        public string? IntegrationType => "notion"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotionApp> _logger;

        private const string BaseUrl = "https://api.notion.com/";

        public const string NotionVersion = "2022-06-28";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public NotionApp(IHttpClientFactory httpClientFactory, ILogger<NotionApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateDatabasePageAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListDatabasesFetcher(this)
            };
        }

        public HttpClient CreateClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("Notion-Version", NotionVersion);
            return client;
        }
    }
}

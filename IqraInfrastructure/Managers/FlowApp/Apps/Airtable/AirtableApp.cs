using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Actions;
using IqraInfrastructure.Managers.FlowApp.Apps.Airtable.Fetchers;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Airtable
{
    public class AirtableApp : IFlowApp
    {
        public string AppKey => "airtable";
        public string Name => "Airtable";
        public string IconUrl => "https://cdn.simpleicons.org/airtable";
        public string? IntegrationType => "airtable"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AirtableApp> _logger;

        private const string BaseUrl = "https://api.airtable.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public AirtableApp(IHttpClientFactory httpClientFactory, ILogger<AirtableApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateRecordAction(this),
                new FindRecordsAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>
            {
                new ListBasesFetcher(this),
                new ListTablesFetcher(this)
            };
        }

        public HttpClient CreateClient(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
    }
}

using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.HubSpot.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.HubSpot
{
    public class HubSpotApp : IFlowApp
    {
        public string AppKey => "hubspot";
        public string Name => "HubSpot";
        public string IconUrl => "https://cdn.simpleicons.org/hubspot";
        public string? IntegrationType => "hubspot"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<HubSpotApp> _logger;

        private const string BaseUrl = "https://api.hubapi.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public HubSpotApp(IHttpClientFactory httpClientFactory, ILogger<HubSpotApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new UpsertContactAction(this),
                new FindContactAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string token)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}

using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Gmail.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Gmail
{
    public class GmailApp : IFlowApp
    {
        public string AppKey => "gmail";
        public string Name => "Gmail";
        public string IconUrl => "https://cdn.simpleicons.org/gmail";
        public string? IntegrationType => "gmail";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GmailApp> _logger;

        private const string BaseUrl = "https://gmail.googleapis.com/";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public GmailApp(IHttpClientFactory httpClientFactory, ILogger<GmailApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new SendEmailAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
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

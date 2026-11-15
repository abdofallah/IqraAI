using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Salesforce.Actions;
using Microsoft.Extensions.Logging;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Salesforce
{
    public class SalesforceApp : IFlowApp
    {
        public string AppKey => "salesforce";
        public string Name => "Salesforce";
        public string IconUrl => "https://cdn.simpleicons.org/salesforce";
        public string? IntegrationType => "salesforce";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SalesforceApp> _logger;

        public const string ApiVersion = "v60.0";

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public SalesforceApp(IHttpClientFactory httpClientFactory, ILogger<SalesforceApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateLeadAction(this),
                new UpsertContactAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient? CreateClient(string accessToken, string? instanceUrl)
        {
            if (string.IsNullOrWhiteSpace(instanceUrl)) return null;

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(instanceUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }
    }
}

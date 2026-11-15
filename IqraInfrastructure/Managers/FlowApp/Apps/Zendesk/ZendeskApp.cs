using IqraCore.Interfaces.FlowApp;
using IqraCore.Interfaces.Integration;
using IqraInfrastructure.Managers.FlowApp.Apps.Zendesk.Actions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace IqraInfrastructure.Managers.FlowApp.Apps.Zendesk
{
    public class ZendeskApp : IFlowApp
    {
        public string AppKey => "zendesk";
        public string Name => "Zendesk";
        public string IconUrl => "https://cdn.simpleicons.org/zendesk";
        public string? IntegrationType => "zendesk"; // Matches Admin Dashboard Definition

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ZendeskApp> _logger;

        public IReadOnlyList<IFlowAction> Actions { get; }
        public IReadOnlyList<IFlowDataFetcher> DataFetchers { get; }

        public ZendeskApp(IHttpClientFactory httpClientFactory, ILogger<ZendeskApp> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            Actions = new List<IFlowAction>
            {
                new CreateTicketAction(this)
            };

            DataFetchers = new List<IFlowDataFetcher>();
        }

        public HttpClient CreateClient(string subdomain, string email, string apiToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://{subdomain}.zendesk.com/");
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{email}/token:{apiToken}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            return client;
        }
    }
}
